using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using TehPers.Discord.TehBot.Commands;
using TehPers.Discord.TehBot.Permissions.Tables;

namespace TehPers.Discord.TehBot.Permissions {

    public class PermissionHandler {

        public BotDatabase Database => Bot.Instance.Database;

        public IQueryable<Role> Roles => this.Database.Roles;

        public IQueryable<Permission> Permissions => this.Database.Permissions;

        public IQueryable<Role> GlobalRoles => from a in this.Database.RoleAssignments
                                               join r in this.Database.Roles on a.RoleID equals r.ID
                                               where a.UserID == null
                                               select r;

        public Task<Role> GetRoleAsync(long? guild, string roleName) => (from r in this.Database.Roles
                                                                         where (r.GuildID == guild || r.GuildID == null) && r.Name == roleName
                                                                         orderby r.GuildID descending
                                                                         select r).FirstOrDefaultAsync();

        public IQueryable<Role> GetRoles(long? guild, long? user) => from r in this.Database.Roles
                                                                     join a in this.Database.RoleAssignments on r.ID equals a.RoleID
                                                                     where (guild == null || r.GuildID == null || r.GuildID == guild) && (a.UserID == null || a.UserID == user)
                                                                     select r;

        public Task<IEnumerable<Role>> GetEffectiveRolesAsync(long? guild, long? user) => this.GetEffectiveRolesAsync(this.GetRoles(guild, user));

        public async Task<IEnumerable<Role>> GetEffectiveRolesAsync(IEnumerable<Role> roles) {
            Queue<Role> adding = new Queue<Role>(roles);
            HashSet<Role> effectiveRoles = new HashSet<Role>();

            while (adding.Any()) {
                Role cur = adding.Dequeue();
                if (effectiveRoles.Any(r => r.ID == cur.ID))
                    continue;

                effectiveRoles.Add(cur);
                int? curID = cur.ParentID;
                if (curID != null)
                    adding.Enqueue(await this.Database.Roles.SingleOrDefaultAsync(r => r.ID == curID));
            }

            return effectiveRoles;
        }

        public async Task<IEnumerable<Role>> GetEffectiveRolesAsync(Role role) {
            // Using a recursive CTE would probably be a lot faster than this
            HashSet<Role> effectiveRoles = new HashSet<Role>();
            Role cur = role;
            while (true) {
                effectiveRoles.Add(cur);
                int? curID = cur.ParentID;
                if (curID == null)
                    break;
                cur = await this.Database.Roles.SingleOrDefaultAsync(r => r.ID == curID);
            }
            return effectiveRoles;
        }

        public async Task<bool> HasPermissionAsync(ulong? guild, ulong user, string permissionName) {
            // TODO
            return permissionName.StartsWith("command.stats") || permissionName.StartsWith("command.skills")
                   || user == 111304027387469824UL || user == 247080708454088705UL
                   ;
            //IEnumerable<Role> roles = await GetEffectiveRolesAsync((long?) guild, (long?) user);
            //return await HasPermissionAsync(roles, permissionName);
        }

        public async Task<bool> HasPermissionAsync(ulong guild, string roleName, string permissionName) {
            Role role = await this.GetRoleAsync((long?) guild, roleName);
            return await this.HasPermissionAsync(role, permissionName);
        }

        public async Task<bool> HasPermissionAsync(Role role, string permissionName) {
            IEnumerable<Role> roles = await this.GetEffectiveRolesAsync(role);
            return await this.HasPermissionAsync(roles, permissionName);
        }

        public async Task<bool> HasPermissionAsync(IEnumerable<Role> roles, string permissionName) {
            foreach (Role effectiveRole in roles)
                if (await this.HasExplicitPermissionAsync(effectiveRole, permissionName))
                    return true;

            return false;
        }

        public async Task<bool> HasExplicitPermissionAsync(Role role, string permissionName) {
            string[] permissionParts = permissionName.Split('.');
            return await this.Database.Permissions.Where(p => p.RoleID == role.ID).ToAsyncEnumerable().Any(p => {
                string[] parts = p.Name.Split('.');

                for (int i = 0; i < parts.Length; i++) {
                    if (parts[i] == "*")
                        return true;
                    if (!string.Equals(parts[i], permissionParts[i], StringComparison.OrdinalIgnoreCase))
                        return false;
                }

                return true;
            });
        }

        public async Task<bool> CreateRoleAsync(ulong? guild, string roleName, string parent = null) {
            // ReSharper disable once ImplicitlyCapturedClosure (parent)
            if (await this.Database.Roles.AnyAsync(r => r.GuildID == (long?) guild && r.Name == roleName))
                return false;

            this.Database.Roles.Add(new Role {
                GuildID = (long?) guild,
                Name = roleName,
                ParentID = parent == null ? null : this.Database.Roles.SingleOrDefault(r => r.GuildID == (long?) guild && r.Name == parent)?.ID
            });

            await this.Database.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteRoleAsync(ulong? guild, string roleName) {
            Role role = await this.GetRoleAsync((long?) guild, roleName);
            if (role == null)
                return false;

            this.Database.Roles.Remove(role);
            this.Database.RoleAssignments.RemoveRange(this.Database.RoleAssignments.Where(a => a.RoleID == role.ID));
            await this.Database.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AssignRoleAsync(ulong? guild, string roleName, ulong? user) {
            Role role = await this.GetRoleAsync((long?) guild, roleName);
            if (role == null)
                return false;

            if (this.Database.RoleAssignments.Any(a => a.RoleID == role.ID && a.UserID == (long?) user))
                return false;

            this.Database.RoleAssignments.Add(new RoleAssignment {
                RoleID = role.ID,
                UserID = (long?) user
            });
            await this.Database.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnassignRoleAsync(ulong? guild, string roleName, ulong? user) {
            Role role = await this.GetRoleAsync((long?) guild, roleName);
            if (role == null)
                return false;

            List<RoleAssignment> assignments = await this.Database.RoleAssignments.Where(a => a.RoleID == role.ID && a.UserID == (long?) user).ToListAsync();
            if (!assignments.Any())
                return false;

            this.Database.RoleAssignments.RemoveRange(assignments);
            await this.Database.SaveChangesAsync();
            return true;
        }

        public async Task<bool> GivePermissionAsync(ulong? guild, string roleName, string permissionName) {
            Role role = await this.GetRoleAsync((long?) guild, roleName);
            if (role == null)
                return false;

            if (this.Database.Permissions.Any(p => p.Name == permissionName && p.RoleID == role.ID))
                return false;

            this.Database.Permissions.Add(new Permission {
                Name = permissionName,
                RoleID = role.ID
            });
            await this.Database.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RevokePermissionAsync(ulong? guild, string roleName, string permissionName) {
            Role role = await this.GetRoleAsync((long?) guild, roleName);
            if (role == null)
                return false;


            List<Permission> permissions = await this.Database.Permissions.Where(p => p.Name == permissionName && p.RoleID == role.ID).ToListAsync();
            if (!permissions.Any())
                return false;

            this.Database.Permissions.RemoveRange(permissions);
            await this.Database.SaveChangesAsync();
            return true;
        }
    }
}
