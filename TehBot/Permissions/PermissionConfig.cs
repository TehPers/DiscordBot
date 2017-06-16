using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TehPers.Discord.TehBot.Permissions {

    public class PermissionConfig {

        public ConcurrentSet<Role> Roles { get; set; } = new ConcurrentSet<Role>();

        public ConcurrentDictionary<string, ConcurrentSet<string>> Permissions { get; set; } = new ConcurrentDictionary<string, ConcurrentSet<string>>();

        public ConcurrentDictionary<string, ConcurrentSet<string>> Users { get; set; } = new ConcurrentDictionary<string, ConcurrentSet<string>>();
    }
}
