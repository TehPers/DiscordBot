using Discord;

namespace TehBot.Core.Commands.Contexts {
    public class CommandContext {
        public IGuild Guild { get; }
        public IChannel Channel { get; }
        public IUser Sender { get; }

        public CommandContext(IGuild guild, IChannel channel, IUser sender) {
            this.Guild = guild;
            this.Channel = channel;
            this.Sender = sender;
        }
    }
}