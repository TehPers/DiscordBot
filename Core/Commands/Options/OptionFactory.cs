using System;
using System.Collections.Generic;
using System.Diagnostics;
using Sprache;
using TehBot.Core.Commands.Contexts;

namespace TehBot.Core.Commands.Options {
    public abstract class OptionFactory {
        public OptionName Name { get; }

        protected OptionFactory(OptionName name) {
            this.Name = name;
        }

        public abstract Parser<IEnumerable<string>> ParseOption(CommandParser parser, CommandContext context, bool shortOption);
    }

    public class ArgumentOptionFactory : OptionFactory {
        public int MinArguments { get; }
        public int? MaxArguments { get; }

        public ArgumentOptionFactory(OptionName name, int minArguments, int? maxArguments) : base(name) {
            Debug.Assert(minArguments > 0);
            Debug.Assert(maxArguments == null || maxArguments > minArguments);

            this.MinArguments = minArguments;
            this.MaxArguments = maxArguments;
        }

        public override Parser<IEnumerable<string>> ParseOption(CommandParser parser, CommandContext context, bool shortOption) {
            throw new NotImplementedException();
        }
    }
}