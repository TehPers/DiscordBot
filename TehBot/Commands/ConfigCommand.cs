using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace TehPers.Discord.TehBot.Commands {
    public class ConfigCommand : Command {
        public ConfigCommand(string name) : base(name) {
            Documentation = new CommandDocs() {
                Description = "Gets or sets a config setting",
                Arguments = new List<CommandDocs.Argument>() {
                    new CommandDocs.Argument("type", "Type of config setting (string | number | bool)"),
                    new CommandDocs.Argument("name", "The name of the config setting"),
                    new CommandDocs.Argument("value", "The value to assign to the config setting", true)
                }
            };
        }

        public override bool Validate(SocketMessage msg, string[] args) {
            if (args.Length < 2)
                return false;

            string type = args[0].ToLower();
            return type == "string" || type == "number" || type == "bool";
        }

        public override async Task Execute(SocketMessage msg, string[] args) {
            Config config = Bot.Instance.Config;
            string type = args[0].ToLower();
            if (args.Length == 2) {
                string output;
                switch (type) {
                    case "string": {
                            output = config.Strings.TryGetValue(args[1], out string value) ? $"{type} `{args[1]}` = `{value}`" : $"{type} `{args[1]}` doesn't exist";
                            break;
                        }

                    case "number": {
                            output = config.Numbers.TryGetValue(args[1], out double value) ? $"{type} `{args[1]}` = `{value}`" : $"{type} `{args[1]}` doesn't exist";
                            break;
                        }

                    case "bool": {
                            output = config.Bools.TryGetValue(args[1], out bool value) ? $"{type} `{args[1]}` = `{value}`" : $"{type} `{args[1]}` doesn't exist";
                            break;
                        }

                    default:
                        output = $"Unknown type {type} (how'd you get this message?)";
                        break;
                }

                await msg.Channel.SendMessageAsync($"{msg.Author.Mention} {output}");
            } else {
                switch (type) {
                    case "string":
                        config.Strings[args[1]] = args[2];
                        break;

                    case "number": {
                            if (double.TryParse(args[2], out double value)) {
                                config.Numbers[args[1]] = value;
                            } else {
                                await msg.Channel.SendMessageAsync($"{msg.Author.Mention} Invalid number format");
                                return;
                            }
                            break;
                        }

                    case "bool": {
                            if (bool.TryParse(args[2], out bool value)) {
                                config.Bools[args[1]] = value;
                            } else {
                                await msg.Channel.SendMessageAsync($"{msg.Author.Mention} Invalid boolean format");
                                return;
                            }
                            break;
                        }

                    default:
                        break;
                }

                Bot.Instance.Save();
                await msg.Channel.SendMessageAsync($"{msg.Author.Mention} Successfully set {type} `{args[1]}` to `{args[2]}`.");
            }
        }

        public override CommandDocs Documentation { get; }
    }
}
