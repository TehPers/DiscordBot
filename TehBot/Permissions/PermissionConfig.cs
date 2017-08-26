using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TehPers.Discord.TehBot.Permissions {

    public class PermissionConfig {

        /// <summary>List of every role</summary>
        public ConcurrentSet<Role> Roles { get; set; } = new ConcurrentSet<Role>();

        /// <summary><seealso cref="ConcurrentDictionary{TKey,TValue}"/> containing the permission name as a key, and a <seealso cref="ConcurrentSet{T}"/> of roles with that permission</summary>
        public ConcurrentDictionary<string, ConcurrentSet<string>> Permissions { get; set; } = new ConcurrentDictionary<string, ConcurrentSet<string>>();

        /// <summary><seealso cref="ConcurrentDictionary{TKey,TValue}"/> of users paired with which roles they have</summary>
        public ConcurrentDictionary<string, ConcurrentSet<string>> Users { get; set; } = new ConcurrentDictionary<string, ConcurrentSet<string>>();
    }
}
