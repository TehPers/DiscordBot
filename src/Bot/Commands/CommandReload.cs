using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bot.Helpers;
using CommandLine;
using Discord;

namespace Bot.Commands {
    public class CommandReload : Command {
        public CommandReload(string name) : base(name) {
            this.WithDescription("Reloads all commands in the bot");
            this.AddVerb<Options>();
        }

        public class Options : Verb {
            [Value(0, HelpText = "The commands to reload. If left out, all commands will be reloaded.", MetaName = "commands")]
            public IEnumerable<string> Cmds { get; set; }

            public override Task Execute(Command cmd, IUserMessage message, string[] args) {
                List<Command> reloadList = new List<Command>();

                List<string> reloadNames = new List<string>();
                if (this.Cmds != null) {
                    reloadNames.AddRange(this.Cmds);
                }

                if (reloadNames.Any()) {
                    reloadList.AddRange(from name in reloadNames
                                        let c = Command.GetCommand(message.GetGuild(), name)
                                        where c != null
                                        select c);
                } else {
                    reloadList.AddRange(Command.CommandRegistry.Values);
                }

                // Unload, then load every command, then send confirmation message
                return Task.WhenAll(reloadList.Select(command => command.Unload().ContinueWith(t => command.Load()))).ContinueWith(t => message.Reply("Commands reloaded"));
            }
        }
    }
}
