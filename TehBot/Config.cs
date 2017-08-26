using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TehPers.Discord.TehBot {
    public class Config {

        [JsonIgnore]
        public SecretConfigs Secrets;

        public ConcurrentDictionary<string, string> Strings { get; set; } = new ConcurrentDictionary<string, string>();

        public ConcurrentDictionary<string, double> Numbers { get; set; } = new ConcurrentDictionary<string, double>();

        public ConcurrentDictionary<string, bool> Bools { get; set; } = new ConcurrentDictionary<string, bool>();

        public class SecretConfigs {

            public string Token { get; set; }
        }
    }
}
