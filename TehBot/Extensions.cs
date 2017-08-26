using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace TehPers.Discord.TehBot {
    public static class Extensions {

        public static Task<IUserMessage> Reply(this IMessage msg, string reply) => msg.Channel.SendMessageAsync($"{msg.Author.Mention} {reply}");
    }
}
