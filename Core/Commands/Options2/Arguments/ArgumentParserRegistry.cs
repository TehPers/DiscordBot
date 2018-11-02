using System;
using System.Collections.Generic;
using Sprache;

namespace TehBot.Core.Commands.Options2.Arguments {
    public class ArgumentParserRegistry {
        private readonly Dictionary<Type, IArgumentParser> _parsers = new Dictionary<Type, IArgumentParser>();

        public bool Register<T>(ArgumentParser<T> parser) {
            if (this._parsers.ContainsKey(typeof(T))) {
                return false;
            }

            this._parsers.Add(typeof(T), parser);
            return true;
        }

        public bool Unregister<T>() {
            return this._parsers.Remove(typeof(T));
        }

        public bool TryGetParser<T>(out ArgumentParser<T> parser) {
            if (this._parsers.TryGetValue(typeof(T), out IArgumentParser rawParser) && rawParser is ArgumentParser<T> genericParser) {
                parser = genericParser;
                return true;
            }

            parser = null;
            return false;
        }
    }
}