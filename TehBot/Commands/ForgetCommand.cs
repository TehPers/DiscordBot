using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace TehPers.Discord.TehBot.Commands {
    public class ForgetCommand : Command {
        public ForgetCommand(string name) : base(name) {
            Documentation = new CommandDocs() {
                Description = "Forgets a remembered command",
                Arguments = new List<CommandDocs.Argument>() {
                    new CommandDocs.Argument("name", "The name of the command to forget")
                }
            };
        }

        public override bool Validate(SocketMessage msg, string[] args) {
            return args.Any();
        }

        public override async Task Execute(SocketMessage msg, string[] args) {
            foreach (Command cmd in Command.CommandList.Values) {
                if (!(cmd is RememberCommand r))
                    continue;

                if (r.RememberedCommands.TryRemove(args.First().ToLower(), out RememberCommand.RCommand rcmd)) {
                    Task send = msg.Channel.SendMessageAsync($"{msg.Author.Mention} Forgot command {args.First().ToLower()}");
                    r.Save();
                    await send;
                } else {
                    await msg.Channel.SendMessageAsync($"{msg.Author.Mention} Unknown command {args.First().ToLower()}");
                }
            }
        }

        public override CommandDocs Documentation { get; }
    }
}
