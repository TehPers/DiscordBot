using TehBot.Core.Commands.Contexts;

namespace TehBot.Core.Commands {
    public abstract class SimpleCommand : Command {
        protected string Name { get; }

        protected SimpleCommand(string name) {
            this.Name = name;
        }

        public override string GetName(CommandContext context) {
            return this.Name;
        }
    }
}