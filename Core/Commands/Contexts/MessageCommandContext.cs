using Discord;

namespace TehBot.Core.Commands.Contexts {
    public class MessageCommandContext : CommandContext {
        public IMessage Source { get; }

        public MessageCommandContext(IMessage source) : base((source.Channel as IGuildChannel)?.Guild, source.Channel, source.Author) {
            this.Source = source;
        }
    }
}