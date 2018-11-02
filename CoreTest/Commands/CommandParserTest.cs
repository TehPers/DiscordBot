using System.Collections.Generic;
using System.Text;
using Discord.WebSocket;
using Sprache;
using TehBot.Core.Commands;
using TehBot.Core.Commands.Options;
using Xunit;

namespace CoreTest.Commands {
    public class CommandParserTest {

        [Fact]
        public void ParseTest() {
            CommandParser commandParser = new CommandParser();
            OptionFactory[] options = {
                new FlagOptionFactory(new OptionName('1', "test1")),
                new ArgumentOptionFactory(new OptionName('2', "test2"), 2, 5)
            };

            Parser<string> argParser = commandParser.ParseArgument();
            Parser<IEnumerable<OptionFactory>> optionParser = commandParser.ParseOptions(null, options);

            Assert.Equal("abcd", argParser.Parse("abcd efg"));
            Assert.Equal("abcd efg", argParser.Parse(@"abcd\ efg"));
            Assert.Equal("abcd", argParser.Parse(@"""abcd"" efg"));
            Assert.Equal("abcd efg", argParser.Parse(@"""abcd efg"""));
            Assert.Equal(@"""abcd", argParser.Parse(@"\""abcd efg\"""));
            Assert.Equal(@"""abcd efg""", argParser.Parse(@"""\""abcd efg\"""""));
            Assert.Throws<ParseException>(() => argParser.Parse(""));
        }
    }
}
