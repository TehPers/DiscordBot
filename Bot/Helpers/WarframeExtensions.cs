using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WarframeNET;

namespace Bot.Helpers {
    public static class WarframeExtensions {
        public static IEnumerable<string> RewardStrings(this Reward reward) {
            return reward.ToStackedItems().Select(Emotes.Emotify);
        }

        public static IEnumerable<string> ImportantRewardStrings(this Reward reward) {
            return reward.ImportantRewards().Select(Emotes.Emotify);
        }

        public static IEnumerable<StackedItem> ImportantRewards(this Reward reward) {
            return reward.ToStackedItems().Where(i => i.IsImportant());
        }

        public static IEnumerable<StackedItem> ToStackedItems(this Reward reward) {
            List<StackedItem> rewards = new List<StackedItem>();
            if (reward.Credits > 0)
                rewards.Add(new StackedItem("credits", reward.Credits));
            rewards.AddRange(reward.Items.Select(i => new StackedItem(i, 1)));
            rewards.AddRange(reward.CountedItems.Select(i => new StackedItem(i)));
            return rewards;
        }

        public static bool IsImportant(this Reward reward) {
            return reward.ToStackedItems().Any(WarframeExtensions.IsImportant);
        }

        public static bool IsImportant(this StackedItem item) => WarframeExtensions.IsRewardImportant(item.Type);
        private static bool IsRewardImportant(string type) {
            return string.Equals(type, "nitain extract", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(type, "kavat genetic code", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(type, "orokin catalyst", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(type, "orokin catalyst blueprint", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(type, "orokin reactor", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(type, "orokin reactor blueprint", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(type, "forma", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(type, "forma blueprint", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(type, "exilus adapter", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(type, "exilus adapter blueprint", StringComparison.OrdinalIgnoreCase)
                   || type.IndexOf("sheev", StringComparison.OrdinalIgnoreCase) != -1
                   || type.IndexOf("wraith", StringComparison.OrdinalIgnoreCase) != -1
                   || type.IndexOf("vandal", StringComparison.OrdinalIgnoreCase) != -1
                   || type.IndexOf("riven", StringComparison.OrdinalIgnoreCase) != -1
                ;
        }

        public static bool IsTraderHere(this WorldState state) => state.WS_VoidTrader.StartTime >= state.Timestamp && state.WS_VoidTrader.EndTime < state.Timestamp;

        public static string Format(this TimeSpan interval) {
            StringBuilder result = new StringBuilder();

            if (interval.Days > 0)
                result.Append($"{interval.Days}d ");
            if (interval.Hours > 0)
                result.Append($"{interval.Hours}h ");
            if (interval.Minutes > 0 || interval.TotalMinutes < 1)
                result.Append($"{Math.Ceiling(interval.TotalMinutes % 60)}m");

            return result.ToString();
        }

        public struct StackedItem {
            public string Type { get; set; }
            public int Count { get; set; }

            public StackedItem(string type) : this(type, 0) { }
            public StackedItem(CountedItem item) : this(item.Type, item.Count) { }
            public StackedItem(string type, int count) {
                this.Type = type;
                this.Count = count;
            }
        }
    }
}
