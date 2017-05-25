using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace TehPers.Discord.TehBot.Commands {
    public class HelpCommand : Command {
        public HelpCommand(string name) : base(name) {
            Documentation = new CommandDocs() {
                Arguments = new List<CommandDocs.Argument>() {
                    new CommandDocs.Argument("command", "The command to get help with", true)
                },
                Description = "Provides help for commands"
            };
        }

        public override bool Validate(SocketMessage msg, string[] args) {
            return true;
        }

        public override async Task Execute(SocketMessage msg, string[] args) {
            if (args.Any() && Command.CommandList.TryGetValue(args.First(), out Command cmd)) {
                await cmd.DisplayUsage(msg.Channel);
            } else {
                await msg.Channel.SendMessageAsync($"Available commands: {string.Join(", ", Command.CommandList.Keys.OrderBy(k => k))}");
            }
        }

        public override CommandDocs Documentation { get; }
    }
}
