using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace BotV2.CommandChecks
{
    /// <summary>
    /// Defines that the usage of this command is always allowed by the owner of the bot.
    /// </summary>
    // TODO: change this to RequireAny(checks...)
    public sealed class RequireOnlyOwnerAttribute : CheckBaseAttribute
    {
        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            return new RequireOwnerAttribute().ExecuteCheckAsync(ctx, help);
        }
    }
}