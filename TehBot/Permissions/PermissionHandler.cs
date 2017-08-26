using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using Discord;
using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json;

namespace TehPers.Discord.TehBot.Permissions {

    public class PermissionHandler {

        private PermissionConfig Config { get; set; }

        private readonly ConcurrentDictionary<string, ConcurrentSet<string>> _effectiveRolesCache = new ConcurrentDictionary<string, ConcurrentSet<string>>();

        public SavingCollection<Role> Roles { get; private set; }

        public PermissionHandler() {
            Bot.Instance.AfterLoaded += AfterLoaded;
        }

        private void AfterLoaded(object sender, EventArgs e) {
            Load();
        }

        public void Load() {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "permissions.json");
            if (File.Exists(path)) {
                Bot.Instance.Log(new LogMessage(LogSeverity.Verbose, "BOT", "Loading permissions"));
                Config = JsonConvert.DeserializeObject<PermissionConfig>(File.ReadAllText(path));
            } else {
                Bot.Instance.Log(new LogMessage(LogSeverity.Verbose, "BOT", "Permissions config not found, creating new permissions config"));
                Config = new PermissionConfig();
                Save();
            }

            Roles = new SavingCollection<Role>(Config.Roles, Save, role => {
                role.Name = role.Name.ToLower();
                return role;
            });

            Roles.Modified += (sender, e) => Save();

            _effectiveRolesCache.Clear();
        }

        private void Save() {
            if (!Directory.Exists(Directory.GetCurrentDirectory()))
                return;

            string path = Path.Combine(Directory.GetCurrentDirectory(), "permissions.json");
            Bot.Instance.Log(new LogMessage(LogSeverity.Verbose, "BOT", "Saving permissions"));
            File.WriteAllText(path, JsonConvert.SerializeObject(Config, Formatting.Indented));
        }

        public bool HasPermission(IUser user, string permission) {
            string discriminator = user.Discriminator;

            if (!Config.Permissions.TryGetValue(permission, out ConcurrentSet<string> pRoles))
                return true;

            if (!Config.Users.TryGetValue(discriminator, out ConcurrentSet<string> uRoles))
                return false;

            return IsAdmin(user) || pRoles.Intersect(GetEffectiveRoles(user.Discriminator)).Any();
        }

        public bool GivePermission(string role, string permission) => GivePermission(Config.Roles.FirstOrDefault(r => r.Name == role), permission);

        public bool GivePermission(Role role, string permission) {
            if (role == null)
                return false;

            Config.Permissions.GetOrAdd(permission, new ConcurrentSet<string>()).Add(role.Name);
            Save();
            return true;
        }

        public bool RemovePermission(string role, string permission) => RemovePermission(Config.Roles.FirstOrDefault(r => r.Name == role), permission);

        public bool RemovePermission(Role role, string permission) {
            if (!Config.Permissions.TryGetValue(permission, out ConcurrentSet<string> pRoles))
                return false;

            pRoles.Remove(role.Name);
            Save();
            return true;
        }

        public Role GetRole(string name) => Config.Roles.FirstOrDefault(role => role.Name == name);

        public void GiveRole(IUser user, string role) {
            if (user == null)
                throw new ArgumentNullException(nameof(user), "Cannot be null");
            role = role?.ToLower() ?? throw new ArgumentNullException(nameof(role), "Cannot be null");

            ConcurrentSet<string> uRoles = Config.Users.GetOrAdd(user.Discriminator, new ConcurrentSet<string>());

            // Make sure this role isn't a child to another role this user has
            if (uRoles.Any(r => GetWithChildren(r).Any(r2 => r2.Name == role)))
                return;

            // Remove all children roles
            foreach (Role child in GetWithChildren(GetRole(role)))
                uRoles.Remove(child.Name);

            // Add the new role
            uRoles.Add(role);

            // Reset the user's cache
            _effectiveRolesCache.TryRemove(user.Discriminator, out ConcurrentSet<string> _);

            // Save
            Save();
        }

        public bool TakeRole(IUser user, string role) {
            if (user == null)
                throw new ArgumentNullException(nameof(user), "Cannot be null");
            role = role?.ToLower() ?? throw new ArgumentNullException(nameof(role), "Cannot be null");

            // Get the user's roles, and return if they have none
            if (!Config.Users.TryGetValue(user.Discriminator, out ConcurrentSet<string> uRoles))
                return false;

            // Remove the role
            bool removed = uRoles.Remove(role);

            // Reset the user's cache
            _effectiveRolesCache.TryRemove(user.Discriminator, out ConcurrentSet<string> _);

            // Save
            Save();

            return removed;
        }

        public void AddRole(Role role) => Roles.Add(role);

        public bool RemoveRole(string name) => RemoveRole(GetRole(name));

        public bool RemoveRole(Role role) {
            // Update children
            foreach (Role child in Roles.ToList().Where(r => r.Parent == role.Name))
                child.Parent = role.Parent;

            // Remove role from users
            foreach (KeyValuePair<string, ConcurrentSet<string>> userKV in Config.Users)
                userKV.Value.Remove(role.Name);

            // Remove permissions from role
            foreach (KeyValuePair<string, ConcurrentSet<string>> permissionKV in Config.Permissions) {
                permissionKV.Value.Remove(role.Name);
            }

            // Remove role
            return Roles.Remove(role);
        }

        public bool IsAdmin(IUser user) => IsAdmin(user.Discriminator);

        public bool IsAdmin(string user) => Config.Users.TryGetValue(user, out ConcurrentSet<string> uRoles) && uRoles.Contains("*");

        public IEnumerable<string> GetRoles(IUser user) => GetRoles(user.Discriminator);

        public IEnumerable<string> GetRoles(string user) => Config.Users.TryGetValue(user, out ConcurrentSet<string> uRoles) ? uRoles.ToList() : Enumerable.Empty<string>();

        public IEnumerable<string> GetEffectiveRoles(IUser user) => GetEffectiveRoles(user.Discriminator);

        private IEnumerable<string> GetEffectiveRoles(string user) {
            // Make sure user exists
            if (!Config.Users.TryGetValue(user, out ConcurrentSet<string> uRoles))
                return Enumerable.Empty<string>();

            // If user is an admin, they have every role
            if (IsAdmin(user))
                return Config.Roles.Select(role => role.Name).ToArray();

            // Check the cache
            if (_effectiveRolesCache.TryGetValue(user, out ConcurrentSet<string> effectiveRoles))
                return effectiveRoles;

            // Find all effective roles
            effectiveRoles = new ConcurrentSet<string>();
            foreach (string roleName in uRoles) {
                Role role = GetRole(roleName);
                if (role == null)
                    continue;

                effectiveRoles.Add(roleName);
                foreach (Role child in GetWithChildren(role))
                    effectiveRoles.Add(child.Name);
            }

            _effectiveRolesCache.AddOrUpdate(user, effectiveRoles, (key, value) => effectiveRoles);
            return effectiveRoles;
        }

        private IEnumerable<Role> GetWithChildren(Role role) => GetWithChildren(role.Name);

        private IEnumerable<Role> GetWithChildren(string role) {
            Queue<string> search = new Queue<string>();
            search.Enqueue(role);
            HashSet<string> children = new HashSet<string>();

            while (search.Any()) {
                string cur = search.Dequeue();
                if (children.Contains(cur))
                    continue;

                children.Add(cur);
                foreach (Role child in Config.Roles.Where(r => r.Parent == cur)) {
                    search.Enqueue(child.Name);
                }
            }

            return children.Select(GetRole).Where(child => child != null);
        }
    }
}
