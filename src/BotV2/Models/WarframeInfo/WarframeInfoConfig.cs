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

        public WarframeInfoEarthConfig? EarthCycle { get; set; }

        public WarframeInfoEarthConfig? CetusCycle { get; set; }
        
        public WarframeInfoVallisConfig? VallisCycle { get; set; }
        
        public WarframeInfoCambionConfig? CambionCycle { get; set; }
    }
}
