using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WarframeNET;

namespace Bot.Helpers
{
    public static class WarframeExtensions
    {
        public static IEnumerable<string> ImportantRewardStrings(this Reward reward) {
            IEnumerable<string> singleRewards = reward.Items.Where(WarframeExtensions.IsRewardImportant);
            IEnumerable<string> countedRewards = reward.CountedItems.Where(item => WarframeExtensions.IsRewardImportant(item.Type)).Select(Emotes.Emotify);
            return singleRewards.Concat(countedRewards);
        }

        /*public static IEnumerable<string> RewardStrings(this Reward reward) {
            List<string> fixedRewards = new List<string>();
            if (reward.Credits > 0)
                fixedRewards.Add($"{reward.Credits}{Emotes.WFCredits}");

            IEnumerable<string> singleRewards = reward.Items;
            IEnumerable<string> countedRewards = reward.CountedItems.Select(Emotes.Emotify);
            return fixedRewards.Concat(singleRewards).Concat(countedRewards);
        }*/

        public static bool IsImportant(this Reward reward) {
            return reward.Items.Any(WarframeExtensions.IsRewardImportant) || reward.CountedItems.Any(items => WarframeExtensions.IsRewardImportant(items.Type));
        }

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
                ;
        }

        public static bool IsTraderHere(this WorldState state) => state.WS_VoidTrader.StartTime >= state.Timestamp && state.WS_VoidTrader.EndTime < state.Timestamp;

        public static string Format(this TimeSpan interval) {
            StringBuilder result = new StringBuilder();

            if (interval.Days > 0)
                result.Append($"{interval.Days}d ");
            if (interval.Hours > 0)
                result.Append($"{interval.Hours}h ");
            //if (interval.Minutes > 0)
            result.Append($"{Math.Ceiling(interval.TotalMinutes % 60)}m");

            return result.ToString();
        }
    }
}
