using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Bot.Commands;
using Discord;
using Discord.WebSocket;
using WarframeNET;

namespace Bot {
    public static class Extensions {
        public static Task<IUserMessage> Reply(this IMessage msg, string reply) => msg.Channel.SendMessageAsync($"{msg.Author.Mention} {reply}");

        public static Task SendToAll(this IEnumerable<IMessageChannel> channels, string message, Embed embed = null) {
            return Task.WhenAll(channels.Select(channel => channel.SendMessageAsync(message, embed: embed)));
        }

        public static IGuild GetGuild(this IMessage msg) => msg.Channel.GetGuild();
        public static IGuild GetGuild(this IChannel channel) => channel is IGuildChannel guildChannel ? guildChannel.Guild : null;

        public static string GetPrefix(this IMessage msg) => msg.Channel.GetGuild().GetPrefix();
        public static string GetPrefix(this IGuild server) => Command.GetPrefix(server);

        public static HashSet<TSource> ToHashSet<TSource>(this IEnumerable<TSource> source) => new HashSet<TSource>(source);

        public static IEnumerable<string> ImportantRewardStrings(this Reward reward) {
            IEnumerable<string> singleRewards = reward.Items.Where(Extensions.IsRewardImportant);
            IEnumerable<string> countedRewards = reward.CountedItems.Where(item => Extensions.IsRewardImportant(item.Type)).Select(Emotes.Emotify);
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
            return reward.Items.Any(Extensions.IsRewardImportant) || reward.CountedItems.Any(items => Extensions.IsRewardImportant(items.Type));
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

        public static string FixPunctuation(this string str) {
            StringBuilder r = new StringBuilder();
            foreach (char c in str) {
                switch (c) {
                    case '\u2013': // en dash
                    case '\u2014': // em dash
                    case '\u2015': // horizontal bar
                        r.Append("-");
                        break;
                    case '\u2017': // double low line
                        r.Append("_");
                        break;
                    case '\u2018': // left single quotation mark
                    case '\u2019': // right single quotation mark
                    case '\u201b': // single high-reversed-9 quotation mark
                    case '\u2032': // prime
                        r.Append("\'");
                        break;
                    case '\u201a': // single low-9 quotation mark
                        r.Append(",");
                        break;
                    case '\u201c': // left double quotation mark
                    case '\u201d': // right double quotation mark
                    case '\u201e': // double low-9 quotation mark
                    case '\u2033': // double prime
                        r.Append("\"");
                        break;
                    case '\u2026': // horizontal ellipsis
                        r.Append("...");
                        break;
                    default:
                        r.Append(c);
                        break;
                }
            }
            return r.ToString();
        }

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

        public static string IfEmpty(this string str, string elseStr) => str == string.Empty ? elseStr : str;
        public static string IfEmpty(this string str, Func<string, string> ifStr, string elseStr) => str == string.Empty ? elseStr : ifStr(str);
    }
}
