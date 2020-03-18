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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BotV2.BotExtensions
{
    internal sealed class CommandBotExtension : BaseExtension, IDisposable
    {
        private readonly CommandsNextExtension _commandsNext;
        private readonly CommandConfigurationService _commandConfiguration;
        private readonly IConfiguration _configuration;

        [SuppressMessage("ReSharper", "SuggestBaseTypeForParameter", Justification = "ILogger is not a registered service")]
        public CommandBotExtension(CommandsNextExtension commands, CommandConfigurationService commandConfiguration, IConfiguration configuration, ILogger<CommandBotExtension> logger, IEnumerable<CommandModuleRegistration> commandModuleRegistrations)
        {
            this._commandsNext = commands ?? throw new ArgumentNullException(nameof(commands));
            this._commandConfiguration = commandConfiguration ?? throw new ArgumentNullException(nameof(commandConfiguration));
            this._configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            using (logger.BeginScope("Module loading"))
            {
                logger.LogInformation("Loading commands");

                foreach (var registration in commandModuleRegistrations)
                {
                    logger.LogTrace($"Registering command module {registration.CommandModuleType.FullName}");
                    this._commandsNext.RegisterCommands(registration.CommandModuleType);
                }
            }

            this._commandsNext.CommandErrored += args =>
            {
                switch (args.Context)
                {
                    case { User: DiscordUser user, Command: Command memberCommand }:
                        logger.LogError(args.Exception, $"An error occurred while {user.Username} ({user.Id}) was executing {memberCommand.QualifiedName}");
                        return args.Context.ShowHelp();
                    case { Command: Command nonMemberCommand }:
                        logger.LogError(args.Exception, $"An error occurred while executing {nonMemberCommand.QualifiedName}");
                        return Task.CompletedTask;
                    default:
                        logger.LogError(args.Exception, "An error occurred while executing a command");
                        return Task.CompletedTask;
                }
            };

            this._commandsNext.CommandExecuted += args =>
            {
                logger.LogTrace($"{args.Context.Member?.Username ?? "??"} ({args.Context.Member?.Id.ToString() ?? "??"}) executed {args.Command.QualifiedName} successfully");
                return Task.CompletedTask;
            };
        }

        protected override void Setup(DiscordClient client)
        {
            if (this.Client != null)
                throw new InvalidOperationException("Extension has already been setup");

            this.Client = client;
            client.MessageCreated += this.OnMessageCreated;
        }

        private async Task OnMessageCreated(MessageCreateEventArgs e)
        {
            if (e.Author.IsBot || e.Channel == null)
            {
                return;
            }

            var msg = e.Message;
            var cmdStart = msg.GetMentionPrefixLength(this.Client.CurrentUser);
            var isMention = cmdStart != -1;
            if (cmdStart == -1)
            {
                var prefixes = new[] {this._configuration["CommandPrefix"] ?? "t!"};
                for (var i = 0; i < prefixes.Length && cmdStart == -1; i++)
                {
                    cmdStart = msg.GetStringPrefixLength(prefixes[i]);
                }
            }

            if (cmdStart == -1)
            {
                return;
            }

            var prefix = msg.Content.Substring(0, cmdStart);
            var invocation = msg.Content.Substring(cmdStart);
            if (!(this._commandsNext.FindCommand(invocation, out var args) is { } cmd))
            {
                return;
            }

            if (cmd.QualifiedName == "help" && !isMention)
            {
                return;
            }

            if (!await this._commandConfiguration.IsCommandEnabled(cmd, msg.Channel.GuildId).ConfigureAwait(false))
            {
                return;
            }

            var context = this._commandsNext.CreateContext(msg, prefix, cmd, args);
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
            this.Client.MessageCreated -= this.OnMessageCreated;
        }
    }
}