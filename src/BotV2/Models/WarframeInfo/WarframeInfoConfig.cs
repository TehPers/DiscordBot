using System.Collections.Generic;

namespace BotV2.Models.WarframeInfo
{
    public class WarframeInfoConfig
    {
        public List<string>? ImportantRewards { get; set; }

        public List<string>? Currencies { get; set; }

        public Dictionary<string, string>? RewardIcons { get; set; }

        public Dictionary<string, string>? RewardThumbnails { get; set; }

        public Dictionary<string, string>? FactionThumbnails { get; set; }

        public string? ActiveColor { get; set; }

        public string? ExpiredColor { get; set; }

        public string? DayColor { get; set; }

        public string? NightColor { get; set; }

        public string? DayIcon { get; set; }

        public string? NightIcon { get; set; }

        public string? DayThumbnail { get; set; }

        public string? NightThumbnail { get; set; }
    }
}
