using System.Collections.Generic;
using System.Linq;
using Sprache;
using TehBot.Core.Commands.Contexts;
using TehBot.Core.Commands.Options;

namespace TehBot.Core.Commands {
    public class FlagOptionFactory : OptionFactory {
        public FlagOptionFactory(OptionName name) : base(name) { }

        public override Parser<IEnumerable<string>> ParseOption(CommandParser parser, CommandContext context, bool shortOption) {
            return Parse.Return(Enumerable.Empty<string>());
        }
    }
}