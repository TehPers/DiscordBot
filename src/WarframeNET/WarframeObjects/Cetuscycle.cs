using System;
using Newtonsoft.Json;

namespace WarframeNET {
    public class CetusCycle : EarthCycle {
        /// <summary>
        /// Whether this is representing the Cetus day/night cycle
        /// </summary>
        public bool IsCetus { get; set; }

        [JsonIgnore]
        [Obsolete("Use 'IsCetus' instead")]
        public bool isCetus { get => this.IsCetus; set => this.IsCetus = value; }
    }
}
