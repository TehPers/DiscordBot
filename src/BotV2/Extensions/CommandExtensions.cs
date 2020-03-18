using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;

namespace BotV2.Extensions
{
    public static class CommandExtensions
    {
        public static Task ShowHelp(this CommandContext context)
        {
            _ = context ?? throw new ArgumentNullException(nameof(context));
            return context.ShowHelp(context.Command);
        }

        public static Task ShowHelp(this CommandContext context, Command command)
        {
            _ = command ?? throw new ArgumentNullException(nameof(command));
            _ = context ?? throw new ArgumentNullException(nameof(context));

            var invocation = $"help {command.QualifiedName}";
            if (!(context.CommandsNext.FindCommand(invocation, out var helpArgs) is { } helpCmd))
            {
                throw new InvalidOperationException("No help command defined");
            }

            var prefix = $"{context.Client.CurrentUser.Mention} ";
            var newContext = context.CommandsNext.CreateFakeContext(context.User, context.Channel, $"{prefix}{invocation}", prefix, helpCmd, helpArgs);
            return context.CommandsNext.ExecuteCommandAsync(newContext);
        }
    }
}