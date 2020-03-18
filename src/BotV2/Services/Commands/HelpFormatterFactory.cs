using System;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using Microsoft.Extensions.DependencyInjection;

namespace BotV2.Services.Commands
{
    public class HelpFormatterFactory<T> : IHelpFormatterFactory
        where T : BaseHelpFormatter
    {
        private readonly IServiceProvider _serviceProvider;

        public HelpFormatterFactory(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public BaseHelpFormatter Create(CommandContext context)
        {
            return ActivatorUtilities.CreateInstance<T>(this._serviceProvider, context);
        }
    }
}