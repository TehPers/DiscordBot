using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using BotV2.Models;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace BotV2.Services
{
    internal sealed class CommandService
    {
        private readonly CommandsNextExtension _commands;
        private readonly ILogger<CommandService> _logger;
        private readonly IEnumerable<CommandModuleRegistration> _commandModuleRegistrations;

        public CommandService(CommandsNextExtension commands, ILogger<CommandService> logger, IEnumerable<CommandModuleRegistration> commandModuleRegistrations)
        {
            this._commands = commands;
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
                var ti = registration.CommandModuleType.GetTypeInfo();
                this._commands.RegisterCommands(registration.CommandModuleType);
            }
        }
    }
}