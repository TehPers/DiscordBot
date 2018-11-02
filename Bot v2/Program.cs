using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Discord;
using Ninject;
using TehBot.Core;
using TehBot.Core.Configuration;
using TehBot.Core.DI;
using TehBot.Core.Logging;

namespace TehBot {
    public class Program {
        private static string Source { get; } = "MAIN";

        public static async Task Main(string[] args) {
            Console.WriteLine("Binding services...");
            IKernel kernal = new StandardKernel(new BotModule());

            // Custom bindings
            kernal.Bind<IKernel>().ToMethod(context => kernal);

            // Configure services
            IProgramConfiguration configuration = kernal.Get<IProgramConfiguration>();
            ILogger logger = kernal.Get<ILogger>();
            logger.LogLevel = configuration.Debug ? LogSeverity.Verbose : LogSeverity.Info;

            // Start the bot
            logger.Info("Starting bot...", Program.Source);
            IBot bot = kernal.Get<IBot>();
            await bot.Start();

            // Pause at end
            logger.Info("Bot closed", Program.Source);
            Console.ReadLine();
        }
    }
}
