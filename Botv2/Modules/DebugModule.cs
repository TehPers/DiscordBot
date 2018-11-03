using Botv2.Implementation.Logging;
using Botv2.Interfaces.Logging;
using Discord;

namespace Botv2.Modules {
    internal class DebugModule : SharedModule {
        public override void Load() {
            base.Load();

            // Set the severity level of the log writers
            this.Bind<LogSeverity>().ToConstant(LogSeverity.Debug).WhenInjectedInto<IAsyncLogWriter>().Named("Severity");
        }
    }

    internal class ProductionModule : SharedModule {
        public override void Load() {
            base.Load();

            // Set the severity level of the log writers
            this.Bind<LogSeverity>().ToConstant(LogSeverity.Info).WhenInjectedInto<IAsyncLogWriter>().Named("Severity");
        }
    }
}