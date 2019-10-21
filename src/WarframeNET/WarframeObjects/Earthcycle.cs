using System;
using Newtonsoft.Json;

namespace WarframeNET
{
    public class EarthCycle
    {
        /// <summary>
        /// ID of the cycle.
        /// </summary>
        public string Id { get; set; }

        [JsonIgnore]
        [Obsolete("Use 'Id' instead")]
        public string id { get => this.Id; set => this.Id = value; }
        
        /// <summary>
        /// When this cycle ends.
        /// </summary>
        public DateTime Expiry { get; set; }
        
        [JsonIgnore]
        [Obsolete("Use 'Expiry' instead")]
        public DateTime expiry { get => this.Expiry; set => this.Expiry = value; }

        /// <summary>
        /// Whether it is currently day.
        /// </summary>
        public bool IsDay { get; set; }

        [JsonIgnore]
        [Obsolete("Use 'IsDay' instead")]
        public bool isDay { get => this.IsDay; set => this.IsDay = value; }

        /// <summary>
        /// How much time is left in this cycle.
        /// </summary>
        public string TimeLeft { get; set; }

        [JsonIgnore]
        [Obsolete("Use 'TimeLeft' instead")]
        public string timeLeft { get => this.TimeLeft; set => this.TimeLeft = value; }

        /// <summary>
        /// String version of whether it is day or night.
        /// </summary>
        [JsonIgnore]
        public string TimeOfDay => this.IsDay ? "Day" : "Night";
    }
}