using System.Collections.Generic;

namespace TehBot.Core.Commands.Options {
    public class OptionWithArguments {
        public OptionFactory Option { get; }
        public IEnumerable<string> Arguments { get; }

        public OptionWithArguments(OptionFactory option, IEnumerable<string> arguments) {
            this.Option = option;
            this.Arguments = arguments;
        }
    }
}