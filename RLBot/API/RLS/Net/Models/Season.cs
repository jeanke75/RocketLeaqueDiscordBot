using System;
using Newtonsoft.Json;
using RLBot.API.RLS.Util;

namespace RLBot.API.RLS.Net.Models
{
    public class Season
    {
        [JsonProperty("seasonId", Required = Required.Always)]
        public int SeasonId { get; set; }

        [JsonProperty("startedOn", Required = Required.Always)]
        public long StartedOnUnix { get; set; }

        [JsonIgnore]
        public DateTimeOffset StartedOn => TimeUtil.UnixTimeStampToDateTime(StartedOnUnix);

        /// <summary>
        ///     <see cref="EndedOnUnix"/> is <code>null</code> if the season has not ended yet.
        /// </summary>
        [JsonProperty("endedOn", Required = Required.AllowNull)]
        public long? EndedOnUnix { get; set; }

        [JsonIgnore]
        public DateTimeOffset EndedOn => EndedOnUnix.HasValue ? TimeUtil.UnixTimeStampToDateTime(EndedOnUnix.Value) : default(DateTimeOffset);
    }
}