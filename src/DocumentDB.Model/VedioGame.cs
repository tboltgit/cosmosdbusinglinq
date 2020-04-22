using Newtonsoft.Json;

namespace DocumentDB.Model
{
    public class VedioGame
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("releaseDate")]
        public string ReleaseDate { get; set; }
        [JsonProperty("engine")]
        public string Engine { get; set; }
        [JsonProperty("platform")]
        public string Platform { get; set; }
    }
}
