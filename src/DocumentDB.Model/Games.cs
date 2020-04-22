using Newtonsoft.Json;
using System.Collections.Generic;

namespace DocumentDB.Model
{
    public class Games
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("location")]
        public string Location { get; set; }
        [JsonProperty("year")]
        public int Year { get; set; }
        [JsonProperty("vedioGames")]
        public List<VedioGame> VedioGames { get; set; }
        [JsonProperty("id")]
        public string Id { get; set; }        
    }
}
