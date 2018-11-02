using Ninject;
using Ninject.Modules;
using TehBot.Core.Configuration;
using TehBot.Core.Logging;

namespace TehBot.Core.DI {
    public class BotModule : NinjectModule {
        public override void Load() {
            this.Bind<IKernel>().ToMethod(context => this.Kernel);

            this.Bind<IBot>().To<Bot>().InSingletonScope();
            this.Bind<IProgramConfiguration>().To<ProgramConfiguration>().InSingletonScope();
            this.Bind<ILogger>().To<ConsoleLogger>().InSingletonScope();
        }
    }
}
