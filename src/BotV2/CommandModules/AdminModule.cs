using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BotV2.Models;
using BotV2.Services.Commands;
using BotV2.Services.Data.Database;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Exceptions;
using StackExchange.Redis;

namespace BotV2.CommandModules
{
    [Group("admin")]
    [RequireOwner]
    public sealed class AdminModule : BaseCommandModule
    {
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
                await context.RespondAsync($"Unknown command {cmdName}");
                return;
            }

            await this._commandConfiguration.SetCommandEnabled(cmd, context.Channel.GuildId, enabled);
            await context.RespondAsync($"{(enabled ? "Enabled" : "Disabled")} {cmd.QualifiedName}");
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
                        await context.RespondAsync("Message link is invalid.");
                        return;
                    }

                    if (!ulong.TryParse(match.Groups["channelId"].Value, out var channelId))
                    {
                        await context.RespondAsync("Unable to parse channel ID.");
                        return;
                    }

                    if (!ulong.TryParse(match.Groups["messageId"].Value, out var messageId))
                    {
                        await context.RespondAsync("Unable to parse message ID.");
                        return;
                    }

                    if (!(await new MessagePointer(messageId, channelId).TryGetMessage(context.Client) is { } message))
                    {
                        await context.RespondAsync("Message not found.");
                        return;
                    }

                    if (message.Author != context.Client.CurrentUser)
                    {
                        await context.RespondAsync("Message was sent by somebody else.");
                        return;
                    }

                    await message.ModifyAsync(content: contents);
                }
                catch (UnauthorizedException)
                {
                    await context.RespondAsync("Insufficient permissions.");
                }
                catch (Exception ex)
                {
                    await context.RespondAsync($"An error occurred:\n```\n{ex}\n```");
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
                        await context.RespondAsync("Message link is invalid.");
                        return;
                    }

                    if (!ulong.TryParse(match.Groups["channelId"].Value, out var channelId))
                    {
                        await context.RespondAsync("Unable to parse channel ID.");
                        return;
                    }

                    if (!ulong.TryParse(match.Groups["messageId"].Value, out var messageId))
                    {
                        await context.RespondAsync("Unable to parse message ID.");
                        return;
                    }

                    if (!(await new MessagePointer(messageId, channelId).TryGetMessage(context.Client) is { } message))
                    {
                        await context.RespondAsync("Message not found.");
                        return;
                    }

                    await message.DeleteAsync();
                }
                catch (UnauthorizedException)
                {
                    await context.RespondAsync("Insufficient permissions.");
                }
                catch (Exception ex)
                {
                    await context.RespondAsync($"An error occurred:\n```\n{ex}\n```");
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
                    var db = await this._dbFactory.GetDatabase();
                    var response = FormatResult(await db.ExecuteAsync(command, args.Cast<object>().ToArray()));
                    await context.RespondAsync($"```\n{response}\n```");
                }
                catch (Exception ex)
                {
                    await context.RespondAsync($"An error occurred:\n```\n{ex.Message}\n```");
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