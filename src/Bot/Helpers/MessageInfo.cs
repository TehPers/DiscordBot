using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace Bot.Helpers {
    public class MessageInfo {
        public ulong MessageID { get; set; }
        public ulong ChannelID { get; set; }

        public async Task<IMessage> GetMessage() {
            IMessageChannel channel = await Bot.Instance.Client.GetMessageChannel(this.ChannelID).ConfigureAwait(false);
            if (channel == null)
                return null;

            return await channel.GetMessageAsync(this.MessageID).ConfigureAwait(false);
        }
    }
}
