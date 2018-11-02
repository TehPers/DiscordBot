using System;
using System.Collections.Generic;
using System.Text;
using Sprache;

namespace TehBot.Core.Commands.Options2.Arguments {
    public abstract class ArgumentParser<T> : IArgumentParser {
        public virtual Parser<object> GetParser() {
            return this.GetGenericParser().Select(e => (object) e);
        }

        public abstract Parser<T> GetGenericParser();
    }
}
