using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bot.Commands;
using Bot.Helpers;
using Discord;
using WarframeNET;

namespace Bot.Extensions {
    public static class Extensions {
        public static string GetPrefix(this IMessage msg) => msg.Channel.GetGuild().GetPrefix();
        public static string GetPrefix(this IGuild server) => Command.GetPrefix(server);

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

                public static string IfEmpty(this string str, string elseStr) => str == string.Empty ? elseStr : str;
        public static string IfEmpty(this string str, Func<string, string> ifStr, string elseStr) => str == string.Empty ? elseStr : ifStr(str);
    }
}
