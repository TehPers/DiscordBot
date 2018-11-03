using System.IO;
using Botv2.Implementation.Logging;
using Botv2.Interfaces.Logging;
using Ninject.Modules;

namespace Botv2.Modules {
    internal abstract class SharedModule : NinjectModule {
        public override void Load() {
            // Write to a log file
            this.Bind<IAsyncLogWriter>().To<FileLogWriter>().InSingletonScope();

            // Write to the console
            this.Bind<IAsyncLogWriter>().To<ConsoleLogWriter>().InSingletonScope();

            // Set directory to write logs to
            this.Bind<string>().ToConstant(Path.Combine(Directory.GetCurrentDirectory(), "logs")).WhenInjectedInto<IAsyncLogWriter>().Named("LogPath");

            // Register a logger
            this.Bind<IAsyncLogger>().To<AsyncLogger>().InSingletonScope();
        }
    }
}