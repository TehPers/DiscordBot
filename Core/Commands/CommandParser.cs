using System;
using System.Collections.Generic;
using System.Linq;
using Sprache;
using TehBot.Core.Commands.Contexts;
using TehBot.Core.Commands.Options;

namespace TehBot.Core.Commands {
    public class CommandParser {
        public virtual Parser<IEnumerable<OptionFactory>> ParseOptions(CommandContext context, OptionFactory[] options) {
            return this.ParseShortOptionList(context, options)
                .Or(this.ParseLongOption(context, options).Once())
                .Token()
                .Many()
                .Select(optionGroups => optionGroups.SelectMany(g => g));
        }

        public virtual Parser<object> ParseSeparator(CommandContext context, OptionFactory[] options) {
            return Parse.WhiteSpace.Select(c => (object) c);
        }

        public virtual Parser<OptionFactory> ParseLongOption(CommandContext context, OptionFactory[] options) {
            return from dashes in Parse.String("--")
                   from name in Parse.LetterOrDigit.AtLeastOnce().Text()
                   let option = options.FirstOrDefault(o => string.Equals(o.Name.LongName, name, StringComparison.OrdinalIgnoreCase)) ?? throw new ParseException($"Unknown option --{name}")
                   from separator in Parse.WhiteSpace.AtLeastOnce()
                   from args in option.ParseOption(this, context, false)
                   select option;
        }

        public virtual Parser<OptionFactory> ParseShortOption(CommandContext context, OptionFactory[] options) {
            return from name in Parse.LetterOrDigit
                   let option = options.FirstOrDefault(o => o.Name.ShortName == name) ?? throw new ParseException($"Unknown option -{name}")
                   from args in option.ParseOption(this, context, true)
                   select option;
        }

        public virtual Parser<IEnumerable<OptionFactory>> ParseShortOptionList(CommandContext context, OptionFactory[] options) {
            return from dash in Parse.Char('-')
                   from optionList in this.ParseShortOption(context, options).AtLeastOnce()
                   select optionList;
        }

        public virtual Parser<string> ParseUnquotedArgument() {
            return Parse.Char('\\').Then(_ => Parse.AnyChar)
                .Or(Parse.AnyChar.Except(Parse.WhiteSpace))
                .AtLeastOnce().Text();
        }

        public virtual Parser<string> ParseQuotedArgument() {
            return from openQuote in Parse.Char('"')
                   from content in Parse.Char('\\').Then(_ => Parse.AnyChar)
                       .Or(Parse.CharExcept('"')).AtLeastOnce().Text()
                   from closeQuote in Parse.Char('"')
                   select content;
        }

        public virtual Parser<string> ParseArgument() {
            return this.ParseQuotedArgument().Or(this.ParseUnquotedArgument());
        }

        public virtual Parser<IEnumerable<string>> ParseArguments(int min = 0, int? max = null) {
            return this.ParseArgument().AtLeastOnce();
        }
    }
}