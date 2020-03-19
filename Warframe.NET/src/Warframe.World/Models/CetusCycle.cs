using System;
using Newtonsoft.Json;

namespace Warframe.World.Models
{
    public class CetusCycle
    {
        [JsonProperty]
        public string Id { get; private set; }
        
        [JsonProperty("activation")]
        public DateTimeOffset ActivatedAt { get; private set; }

        [JsonProperty("expiry")]
        public DateTimeOffset ExpiresAt { get; private set; }

        [JsonProperty]
        public bool IsDay { get; private set; }

        [JsonProperty]
        public string State { get; private set; }

        [JsonProperty]
        public string ShortString { get; private set; }
    }
}
