using System;
using BotV2.Services.Data.Resources.SortedSets;
using Newtonsoft.Json;

namespace BotV2.Models.Data
{
    [JsonObject]
    public class DelayedTaskQueueItem<T> : IScored
    {
        [JsonProperty]
        public T Value { get; }

        [JsonProperty]
        public DateTimeOffset Availabile { get; }

        [JsonIgnore]
        public double Score => this.Availabile.ToUnixTimeSeconds();

        [JsonConstructor]
        public DelayedTaskQueueItem(T value, DateTimeOffset availabile)
        {
            this.Value = value;
            this.Availabile = availabile;
        }
    }
}
