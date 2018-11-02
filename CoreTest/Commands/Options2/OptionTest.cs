using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using TehBot.Core.Commands.Options2;
using TehBot.Core.Commands.Options2.Arguments;
using Xunit;

namespace CoreTest.Commands.Options2 {
    public class OptionTest {

        [Fact]
        public void ParsingTest() {
            MockOption option = new MockOption();
            ArgumentParserRegistry registry = new ArgumentParserRegistry();

            option.AddArgument();

        }

        public class MockOption : Option {
            public override Task Execute(CommandContext context) {
                throw new NotImplementedException();
            }
        }
    }
}
