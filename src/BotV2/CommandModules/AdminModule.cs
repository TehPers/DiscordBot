using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace BotV2.CommandModules
{
    [RequireOwner]
    public sealed class AdminModule : BaseCommandModule
    {
        [Command("echo")]
        [Description("Repeats a message.")]
        [RequireBotPermissions(Permissions.SendMessages)]
        public Task Echo(
            CommandContext context,
            [RemainingText] [Description("The message to say.")] string text
        )
        {
            return context.Channel.SendMessageAsync(text);
        }
    }
}
