using Newtonsoft.Json;

namespace RLBot.API.RLS.Net.Models
{
    public class PlayerRank
    {
        [JsonProperty("rankPoints", Required = Required.Always)]
        public int RankPoints { get; set; }

        [JsonProperty("matchesPlayed")]
        public int? MatchesPlayed { get; set; }

        [JsonProperty("tier")]
        public int? Tier { get; set; }

        [JsonProperty("division")]
        public int? Division { get; set; }
    }
}