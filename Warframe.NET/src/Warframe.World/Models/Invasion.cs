using System;
using Newtonsoft.Json;

namespace Warframe.World.Models
{
    public class Invasion
    {
        [JsonProperty]
        public string Id { get; private set; }

        [JsonProperty]
        public string DefendingFaction { get; private set; }

        [JsonProperty]
        public MissionReward DefenderReward { get; private set; }

        [JsonProperty]
        public string AttackingFaction { get; private set; }

        [JsonProperty]
        public float Completion { get; private set; }

        [JsonProperty]
        public MissionReward AttackerReward { get; private set; }

        [JsonProperty]
        public int Count { get; private set; }

        [JsonProperty]
        public bool Completed { get; private set; }

        [JsonProperty]
        public int RequiredRuns { get; private set; }

        [JsonProperty]
        public bool VsInfestation { get; private set; }

        [JsonProperty]
        public string Node { get; private set; }

        [JsonProperty("activation")]
        public DateTimeOffset ActivatedAt { get; private set; }

        [JsonProperty("desc")]
        public string Description { get; private set; }
    }
}
