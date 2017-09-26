using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace TehPers.Discord.TehBot {
    public static class Extensions {

        public static Task<IUserMessage> Reply(this IMessage msg, string reply) => msg.Channel.SendMessageAsync($"{msg.Author.Mention} {reply}");

        public static SocketGuild GetGuild(this IChannel channel) => Bot.Instance.Client.Guilds.FirstOrDefault(g => g.Channels.Any(c => c.Id == channel.Id));

        public static SocketGuild GetGuild(this IMessage msg) => Bot.Instance.Client.Guilds.FirstOrDefault(g => g.Channels.Any(c => c.Id == msg.Channel.Id));

        public static HashSet<TSource> ToHashSet<TSource>(this IEnumerable<TSource> source) => new HashSet<TSource>(source);

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
    }
}
