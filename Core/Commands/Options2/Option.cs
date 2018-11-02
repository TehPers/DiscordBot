using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Sprache;
using TehBot.Core.Commands.Options2.Arguments;

namespace TehBot.Core.Commands.Options2 {
    public abstract class Option {
        private readonly List<ArgumentSetter> _arguments = new List<ArgumentSetter>();

        public abstract Task Execute(CommandContext context);

        public void AddArgument<T>(ArgumentParserRegistry registery, bool optional, Action<T> set) {
            if (registery.TryGetParser(out ArgumentParser<T> parser)) {
                this._arguments.Add(new ArgumentSetter(o => set((T) o), parser, typeof(T), optional));
            } else {
                throw new ArgumentException($"{typeof(T).FullName} is not registered in {nameof(registery)}", nameof(T));
            }
        }

        public IEnumerable<IArgumentParser> GetArguments() {
            return this._arguments.Select(a => a.Parser);
        }

        public abstract Parser<Action> GetParser(CommandParser parser);

        private class ArgumentSetter {
            public Action<object> OnSet { get; }
            public IArgumentParser Parser { get; }
            public Type ArgumentType { get; }
            public bool Optional { get; }

            public ArgumentSetter(Action<object> onSet, IArgumentParser parser, Type argumentType, bool optional) {
                this.OnSet = onSet;
                this.Parser = parser;
                this.ArgumentType = argumentType;
                this.Optional = optional;
            }
        }
    }

    public class OptionVerb : Option {
        public IList<Option> Options { get; } = new List<Option>();

        public override Task Execute(CommandContext context) {
            throw new NotImplementedException();
        }

        public override Parser<Action> GetParser(CommandParser parser) {
            // On successful parse, return an action that sets all the arguments to their appropriate values
            return this._arguments.Aggregate(Parse.Return<Action>(() => { }), (p, cur) => {
                // Add to the current parser
                return p.Then(action => {
                    // Check if the argument is optional
                    if (cur.Optional) {
                        // Try to parse the current argument (optional)
                        return cur.Parser.GetParser().Optional().Select(result => {
                            if (result.IsDefined) {
                                // Return the previous argument setters followed by the current argument setter
                                return action + (() => cur.OnSet(result));
                            }

                            // Argument wasn't supplied, so ignore it
                            return action;
                        });
                    }

                    // Parse the current argument (required)
                    return cur.Parser.GetParser().Select(result => {
                        // Return the previous argument setters followed by the current argument setter
                        return action + (() => cur.OnSet(result));
                    });
                });
            });
        }
    }
}
