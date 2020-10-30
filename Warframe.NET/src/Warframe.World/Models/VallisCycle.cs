using System;
using Newtonsoft.Json;

namespace Warframe.World.Models
{
    public class VallisCycle
    {
        [JsonProperty]
        public string Id { get; private set; }

        [JsonProperty("activation")]
        public DateTimeOffset ActivatedAt { get; private set; }

        [JsonProperty("expiry")]
        public DateTimeOffset ExpiresAt { get; private set; }

        [JsonProperty]
        public bool IsWarm { get; private set; }

        [JsonProperty]
        public string State { get; private set; }

        [JsonProperty]
        public string ShortString { get; private set; }

        [JsonProperty]
        public string TimeLeft { get; private set; }
    }
}