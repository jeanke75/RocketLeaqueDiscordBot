using Newtonsoft.Json;

namespace RLBot.API.RLS.Net.Models
{
    public class Platform
    {
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; }

        [JsonProperty("name", Required = Required.Always)]
        public string Name { get; set; }
    }
}