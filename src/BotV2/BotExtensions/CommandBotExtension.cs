using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using BotV2.Extensions;
using BotV2.Models;
using BotV2.Services.Commands;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;

namespace BotV2.BotExtensions
{
    internal sealed class CommandBotExtension : BaseExtension, IDisposable
    {
        private readonly CommandsNextExtension _commandsNext;
        private readonly CommandConfigurationService _configService;
        private readonly ILogger<CommandBotExtension> _logger;
        private readonly IEnumerable<CommandModuleRegistration> _registeredCommands;

        [SuppressMessage("ReSharper", "SuggestBaseTypeForParameter", Justification = "ILogger is not a registered service")]
        public CommandBotExtension(CommandsNextExtension commands, CommandConfigurationService commandConfigService, ILogger<CommandBotExtension> logger, IEnumerable<CommandModuleRegistration> commandModuleRegistrations)
        {
            this._commandsNext = commands ?? throw new ArgumentNullException(nameof(commands));
            this._configService = commandConfigService ?? throw new ArgumentNullException(nameof(commandConfigService));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this._registeredCommands = commandModuleRegistrations ?? throw new ArgumentNullException(nameof(commandModuleRegistrations));
        }

        protected override void Setup(DiscordClient client)
        {
            if (this.Client != null)
                throw new InvalidOperationException("Extension has already been setup");

            using (this._logger.BeginScope("Module loading"))
            {
                this._logger.LogInformation("Loading commands");

                foreach (var registration in this._registeredCommands)
                {
                    this._logger.LogTrace($"Registering command module {registration.CommandModuleType.FullName}");
                    this._commandsNext.RegisterCommands(registration.CommandModuleType);
                }
            }

            this.Client = client;
            this._commandsNext.CommandExecuted += this.OnCommandsNextOnCommandExecuted;
            this._commandsNext.CommandErrored += this.OnCommandsNextOnCommandErrored;
            client.MessageCreated += this.OnMessageCreated;
        }

        private Task OnCommandsNextOnCommandExecuted(CommandsNextExtension sender, CommandExecutionEventArgs args)
        {
            this._logger.LogTrace($"{args.Context.Member?.Username ?? "??"} ({args.Context.Member?.Id.ToString() ?? "??"}) executed {args.Command.QualifiedName} successfully");
            return Task.CompletedTask;
        }

        private Task OnCommandsNextOnCommandErrored(CommandsNextExtension sender, CommandErrorEventArgs args)
        {
            switch (args.Context)
            {
                case { User: DiscordUser user, Command: Command memberCommand }:
                    this._logger.LogError(args.Exception, $"An error occurred while {user.Username} ({user.Id}) was executing {memberCommand.QualifiedName}");
                    return args.Context.ShowHelp();
                case { Command: Command nonMemberCommand }:
                    this._logger.LogError(args.Exception, $"An error occurred while executing {nonMemberCommand.QualifiedName}");
                    return Task.CompletedTask;
                default:
                    this._logger.LogError(args.Exception, "An error occurred while executing a command");
                    return Task.CompletedTask;
            }
        }

        private async Task OnMessageCreated(DiscordClient sender, MessageCreateEventArgs e)
        {
            if (e.Author.IsBot || e.Channel == null)
            {
                return;
            }

            // Separate the prefix and the invocation
            var msg = e.Message;
            var cmdStart = msg.GetMentionPrefixLength(this.Client.CurrentUser);
            if (cmdStart == -1)
            {
                await foreach (var p in this._configService.GetPrefixes(e.Guild))
                {
                    cmdStart = msg.GetStringPrefixLength(p);

                    if (cmdStart != -1)
                    {
                        break;
                    }
                }
            }

            if (cmdStart == -1)
            {
                return;
            }

            // Get the command associated with the invocation
            var prefix = msg.Content.Substring(0, cmdStart);
            var invocation = msg.Content.Substring(cmdStart);
            if (!(this._commandsNext.FindCommand(invocation, out var args) is { } cmd))
            {
                return;
            }

            // Check if the command can be executed
            var context = this._commandsNext.CreateContext(msg, prefix, cmd, args);
            if (!await this._configService.CanExecute(context).ConfigureAwait(false))
            {
                return;
            }

            // Execute the command in the background
            _ = Task.Run(async () =>
            {
                try
                {
                    await this._commandsNext.ExecuteCommandAsync(context).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await context.RespondAsync($"An error has occurred:\n```\n{ex.Message}\n```").ConfigureAwait(false);
                }
            });
        }

        public void Dispose()
        {
            this._commandsNext.CommandExecuted -= this.OnCommandsNextOnCommandExecuted;
            this._commandsNext.CommandErrored -= this.OnCommandsNextOnCommandErrored;
            this.Client.MessageCreated -= this.OnMessageCreated;
        }
    }
}