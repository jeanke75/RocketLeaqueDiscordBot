using System.Collections.Generic;
using Newtonsoft.Json;
using RLBot.API.RLS.Data;

namespace RLBot.API.RLS.Net.Models
{
    public class Player
    {
        [JsonProperty("uniqueId", Required = Required.Always)]
        public string UniqueId { get; set; }

        [JsonProperty("displayName", Required = Required.Always)]
        public string DisplayName { get; set; }

        [JsonProperty("platform", Required = Required.Always)]
        public Platform Platform { get; set; }

        [JsonProperty("avatar", Required = Required.AllowNull)]
        public string Avatar { get; set; }

        [JsonProperty("profileUrl", Required = Required.Always)]
        public string ProfileUrl { get; set; }

        [JsonProperty("signatureUrl", Required = Required.Always)]
        public string SignatureUrl { get; set; }

        [JsonProperty("stats", Required = Required.Always)]
        public PlayerStats Stats { get; set; }

        [JsonProperty("rankedSeasons", Required = Required.Always)]
        public Dictionary<RlsSeason, Dictionary<RlsPlaylistRanked, PlayerRank>> RankedSeasons { get; set; }

        [JsonProperty("lastRequested", Required = Required.Always)]
        public uint LastRequested { get; set; }

        [JsonProperty("createdAt", Required = Required.Always)]
        public uint CreatedAt { get; set; }

        [JsonProperty("updatedAt", Required = Required.Always)]
        public uint UpdatedAt { get; set; }

        [JsonProperty("nextUpdateAt", Required = Required.Always)]
        public uint NextUpdateAt { get; set; }
    }
}