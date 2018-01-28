using Newtonsoft.Json;
using RLBot.API.RLS.Data;

namespace RLBot.API.RLS.Net.Requests
{
    public class PlayerBatchRequest
    {
        /// <summary>
        ///     Initialize a request for <see cref="RlsClient.GetPlayersAsync"/>.
        /// </summary>
        /// <param name="platform">The <see cref="RlsPlatform"/> of the player.</param>
        /// <param name="uniqueId">Steam 64 ID / PSN Username / Xbox Gamertag or XUID.</param>
        public PlayerBatchRequest(RlsPlatform platform, string uniqueId)
        {
            Platform = platform;
            UniqueId = uniqueId;
        }

        [JsonProperty("platformId", Required = Required.Always)]
        public RlsPlatform Platform { get; }

        [JsonProperty("uniqueId", Required = Required.Always)]
        public string UniqueId { get; }
    }
}