using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using NLua;
using static KopiLua.Lua;

namespace TehPers.Discord.TehBot.Commands {

    public class ExecuteCommand : Command {

        public ExecuteCommand(string name) : base(name) {
            this.Documentation = new CommandDocs {
                Arguments = new List<CommandDocs.Argument> {
                    new CommandDocs.Argument("code", "The Lua code to execute")
                },
                Description = "Executes a block of Lua code"
            };
        }

        public override bool Validate(SocketMessage msg, string[] args) => args.Any();

        public override async Task Execute(SocketMessage msg, string[] args) {
            string code = string.Join(" ", args);

            try {
                using (Lua interpreter = Bot.Instance.GetInterpreter())
                    interpreter.DoString("exec", code);
            } catch (LuaException ex) {
                await msg.Channel.SendMessageAsync($"```{ex.Message}\n{ex.StackTrace}```");
            } catch (Exception ex) {
                await msg.Channel.SendMessageAsync($"```{ex.Message}\n{ex.StackTrace}```");
            }
        }

        public override CommandDocs Documentation { get; }
    }
}
