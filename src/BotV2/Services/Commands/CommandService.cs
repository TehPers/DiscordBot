using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BotV2.Models;
using BotV2.Services.Data;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BotV2.Services.Commands
{
    internal sealed class CommandService : IDisposable
    {
        private readonly CommandsNextExtension _commandsNext;
        private readonly DiscordClient _client;
        private readonly IDataService _dataService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CommandService> _logger;
        private readonly IEnumerable<CommandModuleRegistration> _commandModuleRegistrations;

        public CommandService(CommandsNextExtension commands, DiscordClient client, IDataService dataService, IConfiguration configuration, ILogger<CommandService> logger, IEnumerable<CommandModuleRegistration> commandModuleRegistrations)
        {
            this._commandsNext = commands ?? throw new ArgumentNullException(nameof(commands));
            this._client = client ?? throw new ArgumentNullException(nameof(client));
            this._dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            this._configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this._commandModuleRegistrations = commandModuleRegistrations ?? throw new ArgumentNullException(nameof(commandModuleRegistrations));

            commands.CommandErrored += args =>
            {
                switch (args.Context)
                {
                    case { User: DiscordUser user, Command: Command memberCommand }:
                        this._logger.LogError(args.Exception, $"An error occurred while {user.Username} ({user.Id}) was executing {memberCommand.QualifiedName}");
                        return Task.CompletedTask;
                    case { Command: Command nonMemberCommand }:
                        this._logger.LogError(args.Exception, $"An error occurred while executing {nonMemberCommand.QualifiedName}");
                        return Task.CompletedTask;
                    default:
                        this._logger.LogError(args.Exception, $"An error occurred while executing a command");
                        return Task.CompletedTask;
                }
            };

            commands.CommandExecuted += args =>
            {
                this._logger.LogTrace($"{args.Context.Member?.Username} ({args.Context.Member?.Id}) executed {args.Command.QualifiedName} successfully");
                return Task.CompletedTask;
            };
        }

        public void Initialize()
        {
            using var scope = this._logger.BeginScope("Module loading");
            this._logger.LogInformation("Loading commands");

            foreach (var registration in this._commandModuleRegistrations)
            {
                this._logger.LogTrace($"Registering command module {registration.CommandModuleType.FullName}");
                this._commandsNext.RegisterCommands(registration.CommandModuleType);
            }

            this._client.MessageCreated += this.OnMessageCreated;
        }

        private async Task OnMessageCreated(MessageCreateEventArgs e)
        {
            if (e.Author.IsBot)
            {
                return;
            }

            var msg = e.Message;
            var cmdStart = msg.GetMentionPrefixLength(this._client.CurrentUser);
            var isMention = cmdStart != -1;
            if (cmdStart == -1)
            {
                var prefixes = new[] { this._configuration["CommandPrefix"] ?? "t!" };
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

            var dataStore = await this._dataService.GetGuildStore(msg.Channel.GuildId);
            if (!await dataStore.AddOrGet($"commands:{cmd.QualifiedName}:enabled", () => true))
            {
                return;
            }

            var context = this._commandsNext.CreateContext(msg, prefix, cmd, args);
            _ = Task.Run(() => this._commandsNext.ExecuteCommandAsync(context));
        }

        public void Dispose()
        {
            this._client.MessageCreated -= this.OnMessageCreated;
        }
    }
}