using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace TehPers.Discord.TehBot.Commands {
    public class ReloadCommand : Command {

        public ReloadCommand(string name) : base(name) {
            Documentation = new CommandDocs() {
                Description = "Reloads the config file and the command list",
                Arguments = new List<CommandDocs.Argument>()
            };
        }
        
        public override bool Validate(SocketMessage msg, string[] args) {
            return true;
        }

        public override async Task Execute(SocketMessage msg, string[] args) {
            Bot.Instance.Load();
            await msg.Channel.SendMessageAsync($"{msg.Author.Mention} Reloaded successfully");
        }

        public override CommandDocs Documentation { get; }
    }
}
