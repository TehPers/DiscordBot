using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BotV2.Models;
using BotV2.Services.Commands;
using BotV2.Services.Data.Database;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using StackExchange.Redis;

namespace BotV2.CommandModules
{
    [Group("admin")]
    [RequireOwner]
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Methods are called via reflection.")]
    public sealed class AdminModule : BaseCommandModule
    {
        private static readonly Regex UserPattern = new Regex(@"^<@(?<id>\d+)>|(?<id>\d+)|(?<name>.+#\d\d\d\d)$");

        private readonly CommandConfigurationService _commandConfiguration;
        private readonly CommandsNextExtension _commandsNext;

        public AdminModule(CommandConfigurationService commandConfiguration, CommandsNextExtension commandsNext)
        {
            this._commandConfiguration = commandConfiguration;
            this._commandsNext = commandsNext;
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
                await context.RespondAsync($"Unknown command {cmdName}").ConfigureAwait(false);
                return;
            }

            await this._commandConfiguration.SetCommandEnabled(cmd, context.Channel.GuildId, enabled).ConfigureAwait(false);
            await context.RespondAsync($"{(enabled ? "Enabled" : "Disabled")} {cmd.QualifiedName}").ConfigureAwait(false);
        }

        [Command("sudo")]
        [Description("Executes a command as another user.")]
        public async Task Sudo(
            CommandContext context,
            [Description("The user to execute the command as.")]
            string user,
            [Description("The command to execute at that user.")] [RemainingText]
            string command
        )
        {
            if (!(this._commandsNext.FindCommand(command, out var args) is { } cmd))
            {
                await context.RespondAsync("Command not found.").ConfigureAwait(false);
                return;
            }

            if (!await this._commandConfiguration.IsCommandEnabled(cmd, context.Channel.GuildId).ConfigureAwait(false))
            {
                await context.RespondAsync("Command is disabled.").ConfigureAwait(false);
                return;
            }

            if (AdminModule.UserPattern.Match(user) is { Success: true } match)
            {
                DiscordUser? newUser = null;
                if (match.Groups["id"] is { Success: true, Value: { } matchedId })
                {
                    if (!ulong.TryParse(matchedId, out var id))
                    {
                        await context.RespondAsync("Invalid user ID.").ConfigureAwait(false);
                        return;
                    }

                    newUser = await context.Client.GetUserAsync(id).ConfigureAwait(false);
                }
                else if (match.Groups["name"] is { Success: true, Value: { } matchedName })
                {
                    newUser = context.Channel.Users.FirstOrDefault(u => string.Equals(matchedName, $"{u.Username}#{u.Discriminator}", StringComparison.OrdinalIgnoreCase));
                    if (newUser is null && context.Channel.Guild is { } guild)
                    {
                        var allUsers = await guild.GetAllMembersAsync().ConfigureAwait(false);
                        newUser = allUsers.FirstOrDefault(u => string.Equals(matchedName, $"{u.Username}#{u.Discriminator}", StringComparison.OrdinalIgnoreCase));
                    }
                }

                if (newUser is null)
                {
                    await context.RespondAsync("Could not find the user.").ConfigureAwait(false);
                    return;
                }

                var newContext = this._commandsNext.CreateFakeContext(newUser, context.Channel, $"{context.User.Mention} {command}", context.User.Mention, cmd, args);
                await this._commandsNext.ExecuteCommandAsync(newContext).ConfigureAwait(false);
            }
        }

        [Group("messages")]
        [Description("Manipulate messages.")]
        public class MessageGroup : BaseCommandModule
        {
            private static readonly Regex UrlPattern = new Regex(@"^https://discordapp\.com/channels/(?<guildId>\d+)/(?<channelId>\d+)/(?<messageId>\d+)$", RegexOptions.IgnoreCase);

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

            [Command("edit")]
            [Description("Edits a message sent by this bot.")]
            public async Task Edit(
                CommandContext context,
                [Description("The link to the message to edit.")]
                string link,
                [RemainingText] string contents
            )
            {
                try
                {
                    if (!(MessageGroup.UrlPattern.Match(link) is { Success: true } match))
                    {
                        await context.RespondAsync("Message link is invalid.").ConfigureAwait(false);
                        return;
                    }

                    if (!ulong.TryParse(match.Groups["channelId"].Value, out var channelId))
                    {
                        await context.RespondAsync("Unable to parse channel ID.").ConfigureAwait(false);
                        return;
                    }

                    if (!ulong.TryParse(match.Groups["messageId"].Value, out var messageId))
                    {
                        await context.RespondAsync("Unable to parse message ID.").ConfigureAwait(false);
                        return;
                    }

                    if (!(await new MessagePointer(messageId, channelId).TryGetMessage(context.Client).ConfigureAwait(false) is { } message))
                    {
                        await context.RespondAsync("Message not found.").ConfigureAwait(false);
                        return;
                    }

                    if (message.Author != context.Client.CurrentUser)
                    {
                        await context.RespondAsync("Message was sent by somebody else.").ConfigureAwait(false);
                        return;
                    }

                    await message.ModifyAsync(content: contents).ConfigureAwait(false);
                }
                catch (UnauthorizedException)
                {
                    await context.RespondAsync("Insufficient permissions.").ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await context.RespondAsync($"An error occurred:\n```\n{ex}\n```").ConfigureAwait(false);
                }
            }

            [Command("delete")]
            [Description("Deletes a message.")]
            public async Task Delete(
                CommandContext context,
                [Description("The link to the message to delete.")]
                string link
            )
            {
                try
                {
                    if (!(MessageGroup.UrlPattern.Match(link) is {Success: true} match))
                    {
                        await context.RespondAsync("Message link is invalid.").ConfigureAwait(false);
                        return;
                    }

                    if (!ulong.TryParse(match.Groups["channelId"].Value, out var channelId))
                    {
                        await context.RespondAsync("Unable to parse channel ID.").ConfigureAwait(false);
                        return;
                    }

                    if (!ulong.TryParse(match.Groups["messageId"].Value, out var messageId))
                    {
                        await context.RespondAsync("Unable to parse message ID.").ConfigureAwait(false);
                        return;
                    }

                    if (!(await new MessagePointer(messageId, channelId).TryGetMessage(context.Client).ConfigureAwait(false) is { } message))
                    {
                        await context.RespondAsync("Message not found.").ConfigureAwait(false);
                        return;
                    }

                    await message.DeleteAsync().ConfigureAwait(false);
                }
                catch (UnauthorizedException)
                {
                    await context.RespondAsync("Insufficient permissions.").ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await context.RespondAsync($"An error occurred:\n```\n{ex}\n```").ConfigureAwait(false);
                }
            }
        }

        [Group("database")]
        [Description("Manipulate the database.")]
        public class DatabaseGroup : BaseCommandModule
        {
            private readonly IDatabaseFactory _dbFactory;

            public DatabaseGroup(IDatabaseFactory dbFactory)
            {
                this._dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
            }

            [Command("execute")]
            [Description("Executes a database command.")]
            [RequireBotPermissions(Permissions.SendMessages)]
            public async Task Execute(
                CommandContext context,
                [Description("The command to send to the database.")]
                string command,
                [Description("The arguments for the command to execute.")] [RemainingText]
                params string[] args
            )
            {
                try
                {
                    var db = await this._dbFactory.GetDatabase().ConfigureAwait(false);
                    var response = FormatResult(await db.ExecuteAsync(command, args.Cast<object>().ToArray()).ConfigureAwait(false));
                    await context.RespondAsync($"```\n{response}\n```").ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await context.RespondAsync($"An error occurred:\n```\n{ex.Message}\n```").ConfigureAwait(false);
                }

                static string FormatResult(RedisResult? result)
                {
                    return result?.Type switch
                    {
                        ResultType.Integer => $"{(long) result:D}",
                        ResultType.SimpleString => $"\"{(string) result}\"",
                        ResultType.BulkString => $"\"{(string) result}\"",
                        ResultType.MultiBulk => $"[{string.Join(", ", ((RedisResult[]) result).Select(FormatResult))}]",
                        ResultType.Error => $"Error: {result}",
                        _ => $"<{result?.ToString() ?? string.Empty}> ({result?.Type.ToString() ?? "<null result>"})"
                    };
                }
            }
        }
    }
}