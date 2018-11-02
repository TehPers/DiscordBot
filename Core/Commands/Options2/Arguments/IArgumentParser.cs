using Sprache;

namespace TehBot.Core.Commands.Options2.Arguments {
    public interface IArgumentParser {
        Parser<object> GetParser();
    }
}