using System;
using System.Diagnostics;
using System.Security;
using System.Threading.Tasks;
using Botv2.Interfaces.Logging;
using Botv2.Modules;
using Discord;
using Ninject;
using Ninject.Planning.Bindings;

namespace Botv2 {
    internal class Program {
        public static async Task Main(string[] args) {
            Console.WriteLine("Bot starting...");

            // Create the IoC container
            using (IKernel kernel = new StandardKernel()) {
                // Load the correct module based on the context of the program
                if (Debugger.IsAttached) {
                    kernel.Load(new DebugModule());
                } else {
                    kernel.Load(new ProductionModule());
                }

                // Create a binding for the bot
                kernel.Bind<Bot>().ToSelf().InSingletonScope();

                try {
                    // Get the logger
                    IAsyncLogger logger = kernel.Get<IAsyncLogger>();

                    // Run the bot
                    do {
                        await logger.Log("Starting bot...", "MAIN", LogSeverity.Info);
                        Bot bot = kernel.Get<Bot>();

                        try {
                            await bot.Run();
                        } catch (Exception ex) {
                            await logger.Log("An uncaught exception occured", "MAIN", LogSeverity.Critical, ex);
                        }

                        await logger.Log("Bot stopped", "MAIN", LogSeverity.Info);
                    } while (!Debugger.IsAttached);
                } catch (Exception ex) {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine("An error occured during initialization:");
                    Console.WriteLine(ex.ToString());
                }
            }
        }
    }
}
