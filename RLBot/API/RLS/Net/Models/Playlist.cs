using Newtonsoft.Json;
using RLBot.API.RLS.Data;

namespace RLBot.API.RLS.Net.Models
{
    public class Playlist
    {
        [JsonProperty("id", Required = Required.Always)]
        public RlsPlaylist Id { get; set; }

        [JsonProperty("platformId", Required = Required.Always)]
        public RlsPlatform Platform { get; set; }

        [JsonProperty("name", Required = Required.Always)]
        public string Name { get; set; }

        [JsonProperty("population", Required = Required.Always)]
        public PlaylistPopulation Population { get; set; }
    }
}