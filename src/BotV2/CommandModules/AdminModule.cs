using System.Threading.Tasks;
using BotV2.Services.Commands;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace BotV2.CommandModules
{
    [RequireOwner]
    [Group("admin")]
    public sealed class AdminModule : BaseCommandModule
    {
        private readonly CommandConfigurationService _commandConfiguration;
        private readonly CommandsNextExtension _commandsNext;

        public AdminModule(CommandConfigurationService commandConfiguration, CommandsNextExtension commandsNext)
        {
            this._commandConfiguration = commandConfiguration;
            this._commandsNext = commandsNext;
        }

        [Command("echo")]
        [Description("Repeats a message.")]
        [RequireBotPermissions(Permissions.SendMessages)]
        public Task Echo(
            CommandContext context,
            [RemainingText] [Description("The message to say.")]
            string text
        )
        {
            return context.Channel.SendMessageAsync(text);
        }

        [Command("setenabled")]
        [Description("Enables or disables a command in this guild.")]
        [RequireGuild]
        public async Task SetEnabled(
            CommandContext context,
            [Description("Whether to enable or disable the command.")]
            bool enabled,
            [RemainingText] [Description("The name of the command.")]
            string cmdName
        )
        {
            if (!(this._commandsNext.FindCommand(cmdName, out _) is { } cmd))
            {
                await context.RespondAsync($"Unknown command {cmdName}");
                return;
            }
            
            await this._commandConfiguration.SetCommandEnabled(cmd, context.Channel.GuildId, enabled);
            await context.RespondAsync($"{(enabled ? "Enabled" : "Disabled")} {cmd.QualifiedName}");
        }

        public Task SetCommandEnabled(
            CommandContext context,
            [Description("The name of the command.")]
            string cmdName,
            [Description("Whether the command should be enabled")]
            bool enabled
        )
        {
            var cmd = this._commandsNext.FindCommand(cmdName, out _);
            return this._commandConfiguration.SetCommandEnabled(cmd, context.Channel.GuildId, enabled);
        }
    }
}
