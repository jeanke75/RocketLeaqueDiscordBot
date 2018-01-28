using Newtonsoft.Json;

namespace RLBot.API.RLS.Net.Models
{
    public class PlayerSearchPage
    {
        [JsonProperty("page", Required = Required.Always)]
        public int Page { get; set; }

        [JsonProperty("results", Required = Required.Always)]
        public int Results { get; set; }

        [JsonProperty("totalResults", Required = Required.Always)]
        public int TotalResults { get; set; }

        [JsonProperty("maxResultsPerPage", Required = Required.Always)]
        public int MaxResultsPerPage { get; set; }

        [JsonProperty("data", Required = Required.Always)]
        public Player[] Data { get; set; }
    }
}