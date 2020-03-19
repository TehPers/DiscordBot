using System.Threading.Tasks;
using BotV2.Services.Commands;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace BotV2.CommandChecks
{
    public class RequireEnabledAttribute : CheckBaseAttribute
    {
        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            if (ctx.Guild is null)
            {
                return Task.FromResult(true);
            }

            var configService = ctx.Services.GetRequiredService<CommandConfigurationService>();
            return configService.IsCommandEnabled(ctx.Command, ctx.Guild.Id);
        }
    }
}