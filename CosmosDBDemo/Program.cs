using DocumentDB;
using DocumentDB.Model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace CosmosDBConsoleApp
{
    class Program
    {
        private static IDatabaseRepository<Games> gamesDBRepository = null;

        static Program()
        {
            string cosmosDbEndPointURL = ConfigurationManager.AppSettings["Cosmosdb.EndpointUrl"];
            string cosmosDbPrimaryKey = ConfigurationManager.AppSettings["Cosmosdb.SASKey"];
            string cosmosDbName = ConfigurationManager.AppSettings["Cosmosdb.Collection.Database.Name"];
            string cosmosDbCollectionName = ConfigurationManager.AppSettings["Cosmosdb.Collection.Games.Name"];

            if (gamesDBRepository == null)
                gamesDBRepository = new DocumentDbRepository<Games>
                    (cosmosDbEndPointURL, cosmosDbPrimaryKey, cosmosDbName, cosmosDbCollectionName);
        }

        static void Main(string[] args)
        {
            bool closeApp = false;

            while (!closeApp)
            {
                Console.WriteLine("1. GetAllGames");
                Console.WriteLine("2. Get All Games using sql query for Ubisoft");
                Console.WriteLine("3. Get Games using Pagniation");
                Console.WriteLine("4. Insert Game");
                Console.WriteLine("5. Delete Game");
                Console.WriteLine("6. Search Game by Name");
                Console.WriteLine("7. Exit");
                Console.WriteLine();
                Console.Write("Enter your choice: ");

                string option = Console.ReadLine().Replace(" ", "");

                var result = Enumerable.Empty<Games>().AsQueryable();

                switch (option)
                {
                    case "1":
                        result = gamesDBRepository.GetAllAsync(null).GetAwaiter().GetResult();

                        if (result.Any())
                            PrintData(result.AsQueryable());
                        break;


                    case "2":
                        string query = "Select * from c WHERE c.name = 'Ubisoft'";
                        result = gamesDBRepository.GetAllAsync(query, null, int.MaxValue, "Ubisoft").GetAwaiter().GetResult().AsQueryable();
                        if (result.Any())
                            PrintData(result.AsQueryable());
                        break;


                    case "3":
                        Console.WriteLine("Pass Limit");
                        int limit = 0;
                        int.TryParse(Console.ReadLine().Replace(" ", ""), out limit);

                        Console.WriteLine("Pass continuation token if any");
                        string token = Console.ReadLine().Replace(" ", "") ?? null;

                        var response = gamesDBRepository.GetManyWithPaginationAsync(token, limit, null, null, null, null).GetAwaiter().GetResult();

                        Console.WriteLine("Continuation token " + response.ResponseContinuation ?? "is Empty");

                        if (result.Any())
                            PrintData(response.AsQueryable());
                        break;


                    case "4":
                        Games game = GetGameData();
                        gamesDBRepository.AddAsync(game, game.Name).GetAwaiter().GetResult();
                        Console.WriteLine("Game Created Successfully");
                        break;


                    case "5":
                        Console.WriteLine("Enter Game Id to Delete : ");
                        string id = Console.ReadLine();
                        Console.WriteLine("Enter its partitionKey : ");
                        string partionKey = Console.ReadLine();
                        gamesDBRepository.DeleteAsync(id, partionKey).GetAwaiter().GetResult();
                        Console.WriteLine("Game Deleted Successfully");
                        break;


                    case "6":
                        Console.WriteLine("Enter any string to search");
                        string searchTerm = Console.ReadLine();

                        Expression<Func<Games, bool>> searchFilter = null;

                        searchFilter = (x => x.Name.ToLower().Contains(searchTerm.ToLower()) ||
                                        x.Location.ToLower().Contains(searchTerm.ToLower()) || 
                                        x.VedioGames.Any(y => y.Name.ToLower().Contains(searchTerm.ToLower()))
                        );

                        result = gamesDBRepository.GetAllAsync(searchFilter).GetAwaiter().GetResult();

                        if (result.Any())
                            PrintData(result);

                        break;


                    case "7": gamesDBRepository.Dispose(); closeApp = true; break;

                    default:
                        Console.WriteLine("Invalid Option Selected");
                        break;
                }

                Console.WriteLine("Press any key continue !!!");
                Console.ReadKey();                
                Console.Clear();
            }           
        }

        #region Private Methods 
        private static Games GetGameData()
        {

            string creator = string.Empty;
            Console.WriteLine("Game Creator : ");
            creator = Console.ReadLine();

            string creatorLocation = string.Empty;
            Console.WriteLine("Game Creator Location : ");
            creatorLocation = Console.ReadLine();

            int year = 0;
            Console.WriteLine("Game Create year : ");
            int.TryParse(Console.ReadLine(), out year);

            string game = string.Empty;
            Console.WriteLine("Game Name : ");
            game = Console.ReadLine();

            string engine = string.Empty;
            Console.WriteLine("Engine");
            engine = Console.ReadLine();

            string releaseDate = string.Empty;
            Console.WriteLine("Release Date : ");
            releaseDate = Console.ReadLine();

            return new Games
            {
                Location = creatorLocation,
                Name = creator,
                Year = year,
                VedioGames = new List<VedioGame>
            {
                new VedioGame
                {
                    Name = game,
                    Engine = engine,
                    ReleaseDate = releaseDate
                }
            }
            };
        }

        private static void PrintData(IQueryable<Games> result)
        {
            if (result == null || !result.Any())
                Console.WriteLine("No games were found");


            foreach (var item in result.GroupBy(x => x.Name, (key, gresult) => new { key, gresult }))
            {
                Console.WriteLine(Environment.NewLine);
                Console.WriteLine("========================================================================");

                Console.WriteLine("Game Creator : " + item.key);

                Console.WriteLine("Years        : " + GetCommaSeperatedStringFromListOfInt(item.gresult.Select(x => x.Year).Distinct().ToList()));

                item.gresult.All(x =>
                {
                    x.VedioGames.All(y =>
                    {
                        Console.WriteLine("----------------------------------------------------------");
                        Console.WriteLine("     Game          => " + y.Name);
                        Console.WriteLine("     Platform      => " + y.Platform ?? "No Platform");
                        Console.WriteLine("     Release Date  => " + y.ReleaseDate);
                        Console.WriteLine("----------------------------------------------------------");
                        return true;
                    });
                    return true;
                });
            }
        }

        private static string GetCommaSeperatedStringFromListOfInt(List<int> serialNumbers)
        {
            string sn = string.Empty;

            if (serialNumbers == null || !serialNumbers.Any())
                return null;

            for (int i = 0; i < serialNumbers.Count(); i++)
            {
                if (i + 1 == serialNumbers.Count())
                {
                    sn += serialNumbers[i];
                }
                else
                    sn += serialNumbers[i] + ",";
            }

            if (string.IsNullOrWhiteSpace(sn))
                return null;

            return sn;
        }
        #endregion
    }
}
