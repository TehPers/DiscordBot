using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using BotV2.Services.Commands;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;

namespace BotV2.CommandModules
{
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Methods are called via reflection.")]
    public sealed class HelpModule : BaseCommandModule
    {
        private readonly CommandConfigurationService _configService;
        private readonly IHelpFormatterFactory _helpFormatterFactory;

        public HelpModule(CommandConfigurationService commandConfigService, IHelpFormatterFactory helpFormatterFactory)
        {
            this._configService = commandConfigService ?? throw new ArgumentNullException(nameof(commandConfigService));
            this._helpFormatterFactory = helpFormatterFactory ?? throw new ArgumentNullException(nameof(helpFormatterFactory));
        }

        [Command("help")]
        [Description("Displays command help.")]
        [RequirePermissions(Permissions.SendMessages)]
        public async Task Help(
            CommandContext context,
            [Description("Command to provide help for.")]
            params string[] command
        )
        {
            var helpFormatter = this._helpFormatterFactory.Create(context);
            var commands = context.CommandsNext.RegisteredCommands.Values.Distinct();

            if (command?.Any() == true)
            {
                // Find the command
                Command? matchedCommand = null;
                var subCommands = (IEnumerable<Command>?) commands;
                foreach (var part in command)
                {
                    if (subCommands is null || matchedCommand?.IsHidden == true)
                    {
                        matchedCommand = null;
                        break;
                    }

                    // ReSharper disable UnreachableCode
                    const bool caseSensitive = false;
                    var nameComparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                    // ReSharper restore UnreachableCode
                    matchedCommand = (Command?) subCommands.FirstOrDefault(cmd => cmd.Aliases.Prepend(cmd.Name).Any(name => string.Equals(cmd.Name, part, nameComparison)));
                    
                    if (matchedCommand is null)
                    {
                        break;
                    }

                    var failedChecks = await this._configService.GetFailedChecks(context, matchedCommand, true).ToListAsync().ConfigureAwait(false);
                    if (failedChecks.Any())
                    {
                        throw new ChecksFailedException(matchedCommand, context, failedChecks);
                    }

                    subCommands = matchedCommand is CommandGroup matchedGroup ? matchedGroup.Children : null;
                }

                if (matchedCommand is null)
                {
                    throw new CommandNotFoundException(string.Join(" ", command));
                }

                helpFormatter.WithCommand(matchedCommand);

                // Add subcommands if any
                if (matchedCommand is CommandGroup group)
                {
                    var eligibleCommands = await GetEligibleCommands(context, this._configService, group.Children.Where(cmd => !cmd.IsHidden)).ToListAsync().ConfigureAwait(false);
                    if (eligibleCommands.Any())
                    {
                        helpFormatter.WithSubcommands(eligibleCommands.OrderBy(cmd => cmd.Name));
                    }
                }
            }
            else
            {
                var eligibleCommands = await GetEligibleCommands(context, this._configService, commands.Where(cmd => !cmd.IsHidden)).ToListAsync().ConfigureAwait(false);
                if (eligibleCommands.Any())
                {
                    helpFormatter.WithSubcommands(eligibleCommands.OrderBy(cmd => cmd.Name));
                }
            }

            var message = helpFormatter.Build();
#pragma warning disable 162 // TODO: Replace with CommandsNextConfiguration.DmHelp if property getters are ever exposed
            // ReSharper disable ConditionIsAlwaysTrueOrFalse
            const bool dmHelp = false;
            if (!dmHelp || context.Channel is DiscordDmChannel || context.Guild is null)
                // ReSharper restore ConditionIsAlwaysTrueOrFalse
            {
                await context.RespondAsync(message.Content, embed: message.Embed).ConfigureAwait(false);
            }
            else
            {
                await context.Member.SendMessageAsync(message.Content, embed: message.Embed).ConfigureAwait(false);
            }
            // ReSharper restore HeuristicUnreachableCode
#pragma warning restore 162

            static IAsyncEnumerable<Command> GetEligibleCommands(CommandContext context, CommandConfigurationService configService, IEnumerable<Command> potentialCommands)
            {
                return potentialCommands.ToAsyncEnumerable().WhereAwait(async cmd => await configService.CanExecute(context, cmd, true).ConfigureAwait(false));
            }
        }
    }
}