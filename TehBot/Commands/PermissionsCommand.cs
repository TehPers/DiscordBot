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
                    new CommandDocs.Argument("arg", "user, parent role, or permission", true)
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
            string arg = args.Length >= 2 ? args[2].ToLower() : null;
            
            // Get their effective roles
            HashSet<string> effectiveRoles = new HashSet<string>(perms.GetEffectiveRoles(user));

            if (action == "add") {
                // Make sure that role doesn't already exist
                if (Bot.Instance.Permissions.Roles.Any(r => r.Name == role))
                    return false;

                // Make sure the user has the given role
                return effectiveRoles.Contains("admin") || effectiveRoles.Contains(role);
            } else if (action == "remove") {
                // Make sure that role doesn't already exist
                if (Bot.Instance.Permissions.Roles.Any(r => r.Name == role))
                    return false;

                // Make sure the user has above the given role
                return effectiveRoles.Contains("admin") || effectiveRoles.Except(perms.GetRoles(user)).Contains(role);
            }
        }

        public override async Task Execute(SocketMessage msg, string[] args) {
            return;
        }

        public override CommandDocs Documentation { get; }
    }
}