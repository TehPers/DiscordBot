using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using TehPers.Discord.TehBot.Permissions;

namespace TehPers.Discord.TehBot.Commands {
    public class PermissionsCommand : Command {

        // Should work like this:
        // .perms <action> <target> [args...]
        // action can be add/remove/assign/unassign/entrust/untrust

        public PermissionsCommand(string name) : base(name) {
            Documentation = new CommandDocs() {
                Description = "Manipulates roles on a user",
                Arguments = new List<CommandDocs.Argument>() {
                    new CommandDocs.Argument("action", "[add] Creates a new role | [remove] Deletes a role | [assign] Assigns a role to a user | [unassign] Unassigns a role from a user | [entrust] Adds a permission to a role | [revoke] Removes a permission from a role | [list] Lists a user's roles"),
                    new CommandDocs.Argument("role", "role to perform the action with"),
                    new CommandDocs.Argument("arg", "user(s), parent role, or permission(s)", true)
                }
            };
        }

        public override bool Validate(SocketMessage msg, string[] args) {
            if (args.Length == 0)
                return false;

            PermissionHandler perms = Bot.Instance.Permissions;
            IUser user = msg.Author;
            string action = args[0].ToLower();

            // Make sure they have permission to use this command
            if (!perms.HasPermission(user, $"{this.ConfigNamespace}.{action}"))
                return false;

            if (action == "list")
                return true;

            if (args.Length < 2)
                return false;

            string role = args[1].ToLower();
            string arg = args.Length > 2 ? args[2].ToLower() : null;

            // Get their effective roles
            HashSet<string> effectiveRoles = new HashSet<string>(perms.GetEffectiveRoles(user));

            switch (action) {
                case "add":
                    // Make sure that role doesn't already exist, parent (if specified) exists, and the user has above the given role
                    if (arg != null && perms.Roles.All(r => r.Name != arg))
                        return false;
                    return perms.Roles.All(r => r.Name != role) && (arg == null || effectiveRoles.Except(perms.GetRoles(user)).Contains(arg));
                case "remove":
                case "assign":
                case "unassign":
                    // Make sure that role exists and the user has above the given role
                    return perms.IsAdmin(user) || effectiveRoles.Except(perms.GetRoles(user)).Contains(role);
                case "entrust":
                case "revoke":
                    // Make sure that the permission name is given
                    if (arg == null)
                        return false;

                    // Make sure that role exists and the user has above the given role
                    return perms.Roles.Any(r => r.Name == role) && effectiveRoles.Except(perms.GetRoles(user)).Contains(role);
                default:
                    return false;
            }
        }

        public override async Task Execute(SocketMessage msg, string[] args) {
            Bot bot = Bot.Instance;
            PermissionHandler perms = bot.Permissions;
            string action = args[0].ToLower();
            string role = args.Length > 1 ? args[1].ToLower() : null;
            string[] cargs = args.Length > 2 ? args.Skip(2).ToArray() : new string[0];

            switch (action) {
                case "add":
                    Role newRole = new Role(role);
                    if (cargs.Length > 0)
                        newRole.Parent = cargs[0];
                    perms.AddRole(newRole);
                    await msg.Reply($"Role \"{role}\" added successfully");
                    break;
                case "remove":
                    if (perms.RemoveRole(role))
                        await msg.Reply($"Role \"{role}\" removed successfully");
                    else
                        await msg.Reply($"Role \"{role}\" doesn't exist");
                    break;
                case "assign":
                    foreach (SocketUser user in msg.MentionedUsers)
                        perms.GiveRole(user, role);
                    await msg.Reply($"Role \"{role}\" assigned to {string.Join(", ", msg.MentionedUsers.Select(user => user.Username))}");
                    break;
                case "unassign":
                    List<string> usersRemoved = (from user in msg.MentionedUsers
                                                 where perms.TakeRole(user, role)
                                                 select user.Username).ToList();
                    await msg.Reply($"Role \"{role}\" assigned to {string.Join(", ", usersRemoved)}");
                    break;
                case "entrust":
                    foreach (string arg in cargs)
                        perms.GivePermission(role, arg);
                    await msg.Reply($"Role \"{role}\" given access to {string.Join(", ", cargs.Select(carg => $"\"{carg}\""))}");
                    break;
                case "revoke":
                    List<string> permsRemoved = (from arg in cargs
                                                 where perms.RemovePermission(role, arg)
                                                 select arg).ToList();
                    await msg.Reply($"Role \"{role}\" assigned to {string.Join(", ", permsRemoved)}");
                    break;
                case "list":
                    await msg.Channel.SendMessageAsync($"{msg.Author.Mention} Roles: {string.Join(", ", perms.Roles.Select(r => r.Name))}");
                    break;
                default:
                    return;
            }
        }

        public override CommandDocs Documentation { get; }
    }
}