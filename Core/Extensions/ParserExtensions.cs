using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Sprache;

namespace TehBot.Core.Extensions {
    public static class ParserExtensions {

        public static Parser<IEnumerable<T>> AtLeast<T>(this Parser<T> parser, int minRepeat) {
            Debug.Assert(parser != null);

            return parser.Repeat(minRepeat).Then(a => parser.Many().Select(a.Concat));
        }

        public static Parser<IEnumerable<T>> AtMost<T>(this Parser<T> parser, int maxRepeat) {
            Debug.Assert(parser != null);
            Debug.Assert(maxRepeat >= 0);

            return i => {
                IInput input = i;
                List<T> results = new List<T>();
                for (IResult<T> result = parser(i); result.WasSuccessful && !input.Equals(result.Remainder); result = parser(input)) {
                    results.Add(result.Value);
                    input = result.Remainder;

                    if (results.Count > maxRepeat) {
                        string message = $"Unexpected '{result.Value}'";
                        string expectation = $"'{string.Join(", ", result.Expectations)}' at most {maxRepeat} times, but found at least {results.Count}";

                        return Result.Failure<IEnumerable<T>>(i, message, new[] { expectation });
                    }
                }

                return Result.Success((IEnumerable<T>) results, input);
            };
        }

        public static Parser<IEnumerable<T>> Between<T>(this Parser<T> parser, int minRepeat, int maxRepeat) {
            Debug.Assert(parser != null);

            return parser.Repeat(minRepeat).Then(a => parser.AtMost(maxRepeat - minRepeat).Select(a.Concat));
        }
    }
}
