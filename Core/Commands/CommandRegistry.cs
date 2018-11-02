using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sprache;
using TehBot.Core.Commands.Contexts;

namespace TehBot.Core.Commands {
    public class CommandRegistry {
        private readonly HashSet<Command> _commands = new HashSet<Command>();

        public void Register(Command command) {
            this._commands.Add(command);
        }

        public void Unregister(Command command) {
            this._commands.Remove(command);
        }

        public Task<bool> Execute(string input, CommandContext context) {
            Parser<Task<bool>> resultParser = this.GetPrefixParser(context).Optional().Then(prefix => {
                if (!prefix.IsDefined) {
                    return Parse.Return(Task.FromResult(false));
                }

                var parser = from command in this.GetCommandParser(context)
                             select command;

                return Parse.Return(Task.FromResult(true));
            });

            return resultParser.Parse(input);
        }

        protected virtual Parser<string> GetPrefixParser(CommandContext context) {
            return Parse.String("!").Text();
        }

        protected virtual Parser<Command> GetCommandParser(CommandContext context) {
            return from name in Parse.AnyChar.Except(Parse.WhiteSpace).AtLeastOnce().Text()
                   let cmd = this._commands.FirstOrDefault(c => string.Equals(c.GetName(context), name, StringComparison.OrdinalIgnoreCase)) ?? throw new ParseException($"Unknown command {name}")
                   select cmd;
        }
    }
}