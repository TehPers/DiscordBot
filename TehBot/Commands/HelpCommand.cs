using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace TehPers.Discord.TehBot.Commands {
    public class HelpCommand : Command {
        public HelpCommand(string name) : base(name) {
            this.Documentation = new CommandDocs() {
                Arguments = new List<CommandDocs.Argument>() {
                    new CommandDocs.Argument("command", "The command to get help with", true)
                },
                Description = "Provides help for commands"
            };
        }

        public override bool Validate(SocketMessage msg, string[] args) => true;

        public override async Task Execute(SocketMessage msg, string[] args) {
            string cmdName = args.FirstOrDefault();
            ulong guild = msg.GetGuild().Id;
            ulong user = msg.Author.Id;

            if (cmdName != null && Command.CommandList.TryGetValue(cmdName, out Command cmd) && await Bot.Instance.Permissions.HasPermissionAsync(guild, user, cmd.ConfigNamespace)) {
                await cmd.DisplayUsage(msg.Channel);
            } else {
                HashSet<string> possibleCommands = new HashSet<string>();
                foreach (Command c in Command.CommandList.Values)
                    if (await Bot.Instance.Permissions.HasPermissionAsync(guild, user, c.ConfigNamespace))
                        possibleCommands.Add(c.Name);

                await msg.Channel.SendMessageAsync($"Available commands: {string.Join(", ", possibleCommands.OrderBy(c => c))}");
            }
        }

        public override bool DefaultPermission => true;
        public override CommandDocs Documentation { get; }
    }
}
