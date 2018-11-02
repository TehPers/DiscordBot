namespace TehBot.Core.Commands.Options {
    public struct OptionName {
        public char? ShortName { get; }
        public string LongName { get; }

        public OptionName(char? shortName, string longName) {
            this.ShortName = shortName;
            this.LongName = longName;
        }
    }
}