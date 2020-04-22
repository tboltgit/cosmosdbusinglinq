# cosmosdbusinglinq
This repo states how cosmos db can be used with c# linq

# What is Cosmos db ? 
https://github.com/tboltgit/cosmosdbusinglinq/tree/master/docs/Azure%20Cosmos%20Db.pptx 

# Getting Started

This repo describes following concept :

    1. How to perform server side queries cosmos db  
    2. How to perform server side pagination 
    3. How to perform search,sorting etc at server side 
    4. How to peform CURD operations 
    
# Usage 
The idea is pretty simple, for how to write code for using comosdb with various operation and taking leverage of repository pattern,generics and less dependent code which describes seperation of concerns so that adding new table/collection or connecting to document client would be done in an easy way.

# Document Client 
https://www.nuget.org/stats/packages/Microsoft.Azure.Documents.Client?groupby=Version

# Example how to get start using cosmos db

            class Employee
            {
                 public string Name {get;set;}
                 public string Designation {get;set;}
            }

            DocumentClient _client = new DocumentClient(new Uri("cosmos db endpoint URI"), "SAS Key", new ConnectionPolicy());
           
            var _databaseId = "Name of the database";
            var _collectionName = "Name of the collection inside database";

            var link = UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionName);

            IQueryable<Games> querySetup = null;

            querySetup = _client.CreateDocumentQuery<Employee>(
                                   link,
                                   new FeedOptions()
                                   {
                                       EnableCrossPartitionQuery = true,
                                       RequestContinuation = null,
                                       PartitionKey =  new PartitionKey("partition_Key") 
                                   });


            var query = querySetup.AsDocumentQuery();

            var result = (await query.ExecuteNextAsync<Games>()).ToList();    
