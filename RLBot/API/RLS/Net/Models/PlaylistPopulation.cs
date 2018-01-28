using System;
using Newtonsoft.Json;
using RLBot.API.RLS.Util;

namespace RLBot.API.RLS.Net.Models
{
    public class PlaylistPopulation
    {
        [JsonProperty("players", Required = Required.Always)]
        public int Players { get; set; }

        [JsonProperty("updatedAt", Required = Required.Always)]
        public long UpdatedAtUnix { get; set; }

        [JsonIgnore]
        public DateTimeOffset UpdatedAt => TimeUtil.UnixTimeStampToDateTime(UpdatedAtUnix);
    }
}