using System;
using WarframeNET;

namespace Bot.Helpers {
    public static class Emotes {
        public static string WFLotus { get; } = "<:WFLotus:380292389534826498>";
        public static string WFCredits { get; } = "<:WFCredits:380292390226886657>";
        public static string WFPlatinum { get; } = "<:WFPlatinum:380292389798936579>";
        public static string WFEndo { get; } = "<:WFEndo:380292389836947458>";
        public static string WFCatalyst { get; } = "<:WFCatalyst:380292389446615042>";
        public static string WFReactor { get; } = "<:WFReactor:380292389882953729>";
        public static string WFForma { get; } = "<:WFForma:380292389836947457>";
        public static string WFExilus { get; } = "<:WFExilus:380292390998638592>";
        public static string WFGrineer { get; } = "<:WFGrineer:384570820191584256>";
        public static string WFCorpus { get; } = "<:WFCorpus:384571252343308288>";

        public static string Emotify(CountedItem item) {
            (string emote, bool currency)? emote = Emotes.EmotifyRaw(item.Type);
            if (emote == null)
                return item.Count == 1 ? item.Type : $"{item.Count}x {item.Type}";
            if (emote.Value.currency)
                return $"{item.Count}{emote.Value.emote}";
            return $"{item.Count}x {emote.Value.emote}";
        }

        public static string Emotify(string item) {
            return Emotes.EmotifyRaw(item)?.emote ?? item;
        }

        public static (string emote, bool currency)? EmotifyRaw(string item) {
            if (item.Equals("credits", StringComparison.OrdinalIgnoreCase))
                return (Emotes.WFCredits, true);
            if (item.Equals("endo", StringComparison.OrdinalIgnoreCase))
                return (Emotes.WFPlatinum, true);
            if (item.Equals("plat", StringComparison.OrdinalIgnoreCase))
                return (Emotes.WFPlatinum, true);
            if (item.Equals("platinum", StringComparison.OrdinalIgnoreCase))
                return (Emotes.WFPlatinum, true);
            if (item.Equals("orokin catalyst", StringComparison.OrdinalIgnoreCase))
                return (Emotes.WFCatalyst, false);
            if (item.Equals("orokin catalyst blueprint", StringComparison.OrdinalIgnoreCase))
                return (Emotes.WFCatalyst, false);
            if (item.Equals("orokin reactor", StringComparison.OrdinalIgnoreCase))
                return (Emotes.WFReactor, false);
            if (item.Equals("orokin reactor blueprint", StringComparison.OrdinalIgnoreCase))
                return (Emotes.WFReactor, false);
            if (item.Equals("forma", StringComparison.OrdinalIgnoreCase))
                return (Emotes.WFForma, false);
            if (item.Equals("forma blueprint", StringComparison.OrdinalIgnoreCase))
                return (Emotes.WFForma, false);
            if (item.Equals("exilus adapter", StringComparison.OrdinalIgnoreCase))
                return (Emotes.WFExilus, false);
            if (item.Equals("exilus adapter blueprint", StringComparison.OrdinalIgnoreCase))
                return (Emotes.WFExilus, false);

            return null;
        }
    }
}
