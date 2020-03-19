using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;

namespace BotV2.Services.Commands
{
    public interface IHelpFormatterFactory
    {
        public BaseHelpFormatter Create(CommandContext context);
    }
}