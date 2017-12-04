using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bot.Helpers;
using CommandLine;
using Discord;

namespace Bot.Commands {
    public class CommandHelp : Command {
        public CommandHelp(string name) : base(name) {
            this.AddVerb<Options>();
            this.WithDescription("Shows help about a command, or lists all available commands");
        }

        protected override bool IsDefaultEnabled { get; } = true;

        public class Options : Verb {
            [Value(0, HelpText = "The name of the command", MetaName = "command")]
            public string Cmd { get; set; }

            public override async Task Execute(Command helpCmd, IMessage message, string[] args) {
                // Try to show command help
                Command cmd = Command.GetCommand(message.GetGuild(), this.Cmd);
                if (this.Cmd != null && cmd != null)
                    await cmd.ShowHelp(message, Enumerable.Empty<string>()).ConfigureAwait(false);

                // List commands
                IEnumerable<Command> cmds = await Command.AvailableCommands(message.Channel.GetGuild(), message.Author).ConfigureAwait(false);
                await message.Reply(string.Join(", ", cmds.Select(c => c.GetName(message.Channel.GetGuild())))).ConfigureAwait(false);
            }
        }
    }
}
