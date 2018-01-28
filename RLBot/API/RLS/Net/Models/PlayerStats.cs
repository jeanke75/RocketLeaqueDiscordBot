using Newtonsoft.Json;

namespace RLBot.API.RLS.Net.Models
{
    public class PlayerStats
    {
        [JsonProperty("wins", Required = Required.Always)]
        public int Wins { get; set; }

        [JsonProperty("goals", Required = Required.Always)]
        public int Goals { get; set; }

        [JsonProperty("mvps", Required = Required.Always)]
        public int Mvps { get; set; }

        [JsonProperty("saves", Required = Required.Always)]
        public int Saves { get; set; }

        [JsonProperty("shots", Required = Required.Always)]
        public int Shots { get; set; }

        [JsonProperty("assists", Required = Required.Always)]
        public int Assists { get; set; }
    }
}