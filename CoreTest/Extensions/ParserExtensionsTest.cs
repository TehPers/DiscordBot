using System;
using System.Collections.Generic;
using System.Text;
using Sprache;
using TehBot.Core.Extensions;
using Xunit;

namespace CoreTest.Extensions {
    public class ParserExtensionsTest {

        [Fact]
        public void BetweenTest() {
            Parser<string> parser = Parse.AnyChar.Between(2, 4).End().Text();

            Assert.Throws<ParseException>(() => parser.Parse("a"));
            Assert.Equal("ab", parser.Parse("ab"));
            Assert.Equal("abc", parser.Parse("abc"));
            Assert.Equal("abcd", parser.Parse("abcd"));
            Assert.Throws<ParseException>(() => parser.Parse("abcde"));
        }
    }
}
