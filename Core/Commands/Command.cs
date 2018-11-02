using System.Collections.Generic;
using System.Threading.Tasks;
using TehBot.Core.Commands.Contexts;
using TehBot.Core.Commands.Options;

namespace TehBot.Core.Commands {
    public abstract class Command {
        protected Command() {

        }

        public abstract string GetName(CommandContext context);

        // TODO
        public abstract Task Execute(object options, CommandContext context);

        public abstract IEnumerable<OptionFactory> GetOptions(CommandContext context);
    }
}
