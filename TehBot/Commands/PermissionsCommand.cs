using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using TehPers.Discord.TehBot.Permissions;
using TehPers.Discord.TehBot.Permissions.Tables;

namespace TehPers.Discord.TehBot.Commands {
    public class PermissionsCommand : Command {

        public PermissionsCommand(string name) : base(name) {
            this.Documentation = new CommandDocs() {
                Description = "Manipulates roles on a user",
                Arguments = new List<CommandDocs.Argument>() {
                    new CommandDocs.Argument("action", "[create] Creates a new role | [delete] Deletes a role | [assign] Assigns a role to a user | [unassign] Unassigns a role from a user | [entrust] Adds a permission to a role | [revoke] Removes a permission from a role | [list] Lists a user's roles"),
                    new CommandDocs.Argument("role", "role to perform the action with"),
                    new CommandDocs.Argument("arg", "user(s), parent role, or permission(s)", true)
                }
            };
        }

        public override bool Validate(SocketMessage msg, string[] args) {
            if (args.Length == 0)
                return false;

            PermissionHandler perms = Bot.Instance.Permissions;
            string action = args[0].ToLower();
            SocketUser user = msg.Author;
            SocketGuild guild = msg.GetGuild();

            if (!perms.HasPermissionAsync(guild.Id, user.Id, $"{this.ConfigNamespace}.{action}").Result)
                return false;

            switch (action) {
                case "list":
                    // No arguments needed
                    return true;
                case "create":
                case "delete":
                    // Role is needed
                    return args.Length > 1;
                case "assign":
                case "unassign":
                case "entrust":
                case "revoke":
                    // An argumnt after role is needed
                    return args.Length > 2;
                default:
                    return false;
            }
        }

        public override async Task Execute(SocketMessage msg, string[] args) {
            Bot bot = Bot.Instance;
            PermissionHandler perms = bot.Permissions;
            string action = args[0].ToLower();
            string role = args.Length > 1 ? args[1] : null;
            string[] cargs = args.Length > 2 ? args.Skip(2).ToArray() : new string[0];
            long guild = (long) msg.GetGuild().Id;

            switch (action) {
                case "create":
                    if (await perms.CreateRoleAsync((ulong) guild, role, cargs.FirstOrDefault()))
                        await msg.Reply($"Role \"{role}\" created successfully");
                    else
                        await msg.Reply($"Role \"{role}\" already exists");
                    break;
                case "delete":
                    if (await perms.DeleteRoleAsync(msg.GetGuild().Id, role))
                        await msg.Reply($"Role \"{role}\" deleted successfully");
                    else
                        await msg.Reply($"Role \"{role}\" doesn't exist");
                    break;
                case "assign":
                    List<string> usersAssigned = new List<string>();
                    foreach (SocketUser user in msg.MentionedUsers)
                        if (await perms.AssignRoleAsync((ulong) guild, role, user.Id))
                            usersAssigned.Add($"{user.Mention}");

                    if (usersAssigned.Any())
                        await msg.Reply($"Role \"{role}\" assigned to {string.Join(", ", usersAssigned)}");
                    else
                        await msg.Reply($"Role \"{role}\" assigned to nobody");
                    break;
                case "unassign":
                    List<string> usersUnassigned = new List<string>();
                    foreach (SocketUser user in msg.MentionedUsers)
                        if (await perms.UnassignRoleAsync((ulong) guild, role, user.Id))
                            usersUnassigned.Add($"{user.Mention}");

                    if (usersUnassigned.Any())
                        await msg.Reply($"Role \"{role}\" removed from {string.Join(", ", usersUnassigned)}");
                    else
                        await msg.Reply($"Role \"{role}\" removed from nobody");
                    break;
                case "entrust":
                    List<string> permissionsAdded = new List<string>();
                    foreach (string arg in cargs)
                        if (await perms.GivePermissionAsync((ulong) guild, role, arg))
                            permissionsAdded.Add(arg);

                    if (permissionsAdded.Any())
                        await msg.Reply($"Permissions {string.Join(", ", permissionsAdded)} added for role \"{role}\"");
                    else
                        await msg.Reply($"No permissions added for role \"{role}\"");
                    break;
                case "revoke":
                    List<string> permissionsRevoked = new List<string>();
                    foreach (string arg in cargs)
                        if (await perms.RevokePermissionAsync((ulong) guild, role, arg))
                            permissionsRevoked.Add(arg);

                    if (permissionsRevoked.Any())
                        await msg.Reply($"Permissions {string.Join(", ", permissionsRevoked)} revoked for role \"{role}\"");
                    else
                        await msg.Reply($"No permissions revoked for role \"{role}\"");
                    break;
                case "list":
                    SocketUser target = msg.MentionedUsers.FirstOrDefault();

                    List<Role> roles = await perms.GetRoles(guild, (long?) target?.Id).ToListAsync();
                    List<Role> effectiveRoles = (await perms.GetEffectiveRolesAsync(roles)).ToList();

                    HashSet<int> globalRoles = effectiveRoles.Select(r => r.ID).Intersect(perms.GlobalRoles.Select(r => r.ID)).ToHashSet();
                    HashSet<int> inheritedRoles = effectiveRoles.Select(r => r.ID).Except(roles.Select(r => r.ID)).ToHashSet();

                    IEnumerable<string> roleStrings = effectiveRoles.OrderBy(r => {
                        if (globalRoles.Contains(r.ID))
                            return 1;
                        if (inheritedRoles.Contains(r.ID))
                            return 0;
                        return -1;
                    }).Select(r => {
                        string s = r.Name;
                        if (!globalRoles.Contains(r.ID))
                            s = $"**{s}**";
                        if (inheritedRoles.Contains(r.ID))
                            s = $"_{s}_";
                        return s;
                    });
                    await msg.Channel.SendMessageAsync($"{msg.Author.Mention} Roles for {target?.Mention ?? "everyone"}: {string.Join(", ", roleStrings)}");
                    break;
                default:
                    return;
            }
        }

        public override CommandDocs Documentation { get; }
    }
}