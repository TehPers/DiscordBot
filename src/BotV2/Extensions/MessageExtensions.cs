using System;
using System.Threading;
using System.Threading.Tasks;
using BotV2.Models;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;

namespace BotV2.Extensions
{
    public static class MessageExtensions
    {
        public static async Task<Option<DiscordMessage>> TryModifyAsync(this DiscordMessage message, Optional<string> content = default, Optional<DiscordEmbed> embed = default)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));

            try
            {
                return new Option<DiscordMessage>(await message.ModifyAsync(content, embed).ConfigureAwait(false));
            }
            catch (NotFoundException)
            {
                return default;
            }
        }

        public static async Task<bool> TryDeleteAsync(this DiscordMessage message, string? reason = default)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));

            try
            {
                await message.DeleteAsync(reason).ConfigureAwait(false);
                return true;
            }
            catch (NotFoundException)
            {
                return false;
            }
        }

        public static async Task<bool> TryPinAsync(this DiscordMessage message, bool catchUnauthorized = false)
        {
            try
            {
                await message.PinAsync().ConfigureAwait(false);
                return true;
            }
            catch (UnauthorizedException) when (catchUnauthorized)
            {
                return false;
            }
            catch (NotFoundException)
            {
                return false;
            }
        }

        public static async Task<bool> TryPinSilentlyAsync(this DiscordMessage message, DiscordClient client, bool catchUnauthorized = false)
        {
            _ = client ?? throw new ArgumentNullException(nameof(client));
            _ = message ?? throw new ArgumentNullException(nameof(message));

            try
            {
                // Wait for pin message
                // If an error occurs with pinning the message, tokenSource is automatically disposed so the task will not continue to run
                using var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var cancellation = tokenSource.Token;
                var deletePinMessage = Task.Run(async () =>
                {
                    var notification = await client.WaitForMessageAsync(msg => msg.Author == client.CurrentUser && msg.Content == string.Empty, cancellation).ConfigureAwait(false);
                    try
                    {
                        await notification.DeleteAsync().ConfigureAwait(false);
                    }
                    catch (UnauthorizedException) when (catchUnauthorized)
                    {
                    }
                    catch (NotFoundException)
                    {
                    }
                }, cancellation);

                // Pin the message
                await message.PinAsync().ConfigureAwait(false);
                await Task.WhenAny(deletePinMessage, Task.Delay(TimeSpan.FromSeconds(5), cancellation)).ConfigureAwait(false);

                return true;
            }
            catch (UnauthorizedException) when (catchUnauthorized)
            {
                return false;
            }
            catch (NotFoundException)
            {
                return false;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (BadRequestException)
            {
                return false;
            }
        }
    }
}