using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace BotV2.CommandChecks
{
    public class HelpRequireMentionAttribute : CheckBaseAttribute
    {
        private static readonly Regex MentionPattern = new Regex(@"^\s*<@!?(?<id>\d+)>\s*$");

        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            if (!string.Equals(ctx.Command.QualifiedName, "help", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(true);
            }

            if (!(HelpRequireMentionAttribute.MentionPattern.Match(ctx.Prefix) is { Success: true } match))
            {
                return Task.FromResult(false);
            }

            if (!(match.Groups["id"] is { Success: true, Value: string matchedId } && ulong.TryParse(matchedId, out var id)))
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(id == ctx.Client.CurrentUser.Id);
        }
    }
}