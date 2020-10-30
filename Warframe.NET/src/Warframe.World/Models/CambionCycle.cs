using System;
using Newtonsoft.Json;

namespace Warframe.World.Models
{
    public class CambionCycle
    {
        [JsonProperty]
        public string Id { get; private set; }

        [JsonProperty("activation")]
        public DateTimeOffset ActivatedAt { get; private set; }

        [JsonProperty("expiry")]
        public DateTimeOffset ExpiresAt { get; private set; }

        [JsonIgnore]
        public bool IsFass => string.Equals(this.Active, "fass");

        [JsonProperty]
        public string Active { get; private set; }
    }
}