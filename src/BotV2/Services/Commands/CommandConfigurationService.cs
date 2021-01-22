using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BotV2.CommandChecks;
using BotV2.Services.Data;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Configuration;

namespace BotV2.Services.Commands
{
    public class CommandConfigurationService
    {
        private readonly IDataService _dataService;
        private readonly IConfiguration _configuration;
        private readonly IEnumerable<CheckBaseAttribute> _commandChecks;

        public CommandConfigurationService(IDataService dataService, IConfiguration configuration, IEnumerable<CheckBaseAttribute> commandChecks)
        {
            this._dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            this._configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this._commandChecks = commandChecks ?? throw new ArgumentNullException(nameof(commandChecks));
        }

        public async Task<bool> IsCommandEnabled(Command command, ulong guildId)
        {
            var dataStore = this._dataService.GetGuildStore(guildId);
            var resource = dataStore.GetObjectResource<bool>($"commands:{command.QualifiedName}:enabled");
            if ((await resource.Get().ConfigureAwait(false)).TryGetValue(out var enabled))
            {
                return enabled;
            }

            return true;
        }

        public async Task SetCommandEnabled(Command command, ulong guildId, bool enabled)
        {
            var dataStore = this._dataService.GetGuildStore(guildId);
            var resource = dataStore.GetObjectResource<bool>($"commands:{command.QualifiedName}:enabled");
            await resource.Set(enabled).ConfigureAwait(false);
        }

        public IAsyncEnumerable<string> GetPrefixes(DiscordGuild? guild)
        {
            // TODO: prefix dependent on guild
            return new[] {this._configuration["CommandPrefix"] ?? "t!"}.ToAsyncEnumerable();
        }

        public async ValueTask<bool> CanExecute(CommandContext parentContext, Command command, bool isHelp = false)
        {
            return !await this.GetFailedChecks(parentContext, command, isHelp).AnyAsync().ConfigureAwait(false);
        }

        public async ValueTask<bool> CanExecute(CommandContext context, bool isHelp = false)
        {
            return !await this.GetFailedChecks(context, isHelp).AnyAsync().ConfigureAwait(false);
        }

        public IAsyncEnumerable<CheckBaseAttribute> GetFailedChecks(CommandContext parentContext, Command command, bool isHelp = false)
        {
            var prefix = $"{parentContext.Client.CurrentUser.Mention} ";
            var childContext = parentContext.CommandsNext.CreateFakeContext(parentContext.User, parentContext.Channel, $"{prefix}{command.QualifiedName}", prefix, command);
            return this.GetFailedChecks(childContext, isHelp);
        }

        public async IAsyncEnumerable<CheckBaseAttribute> GetFailedChecks(CommandContext context, bool isHelp = false)
        {
            var checks = this.GetExecutionChecks(context.Command).ToList();
            if (checks.OfType<RequireOnlyOwnerAttribute>().FirstOrDefault() is { } requireOnlyOwner && await requireOnlyOwner.ExecuteCheckAsync(context, isHelp).ConfigureAwait(false))
            {
                yield break;
            }

            foreach (var check in checks)
            {
                if (!await check.ExecuteCheckAsync(context, isHelp).ConfigureAwait(false))
                {
                    yield return check;
                }
            }
        }

        public IEnumerable<CheckBaseAttribute> GetExecutionChecks(Command command)
        {
            return this._commandChecks.Concat(command.ExecutionChecks);
        }
    }
}