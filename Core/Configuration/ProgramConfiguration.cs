namespace TehBot.Core.Configuration {
    public class ProgramConfiguration : IProgramConfiguration {
        public bool Debug { get; } = false;

        public ProgramConfiguration() {
#if DEBUG
            this.Debug = true;
#endif
        }
    }
}
