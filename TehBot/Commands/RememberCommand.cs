using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.WebSocket;
using Newtonsoft.Json;
using NLua;
using NLua.Exceptions;

namespace TehPers.Discord.TehBot.Commands {
    public class RememberCommand : Command {
        public RememberCommand(string name) : base(name) {
            this.Documentation = new CommandDocs() {
                Description = "Remembers commands that can be used in the future",
                Arguments = new List<CommandDocs.Argument>() {
                    new CommandDocs.Argument("name", "Name of the command to create"),
                    new CommandDocs.Argument("-l", "Flag to execute the command through the Lua interpreter", true),
                    new CommandDocs.Argument("contents", "Contents of the command. Use %var% to grab variables from the Lua global table in non-Lua commands, and %% to escape percents.")
                }
            };

            Bot.Instance.Client.MessageReceived += this.MessageReceivedAsync;
            this.Load();

            //Interpreter.DoString("setmetatable(_G, {__index = function(t, k) if k:lower() == \"time\" then return os.time() end })", "setup");
        }

        public override void Unload() {
            Bot.Instance.Client.MessageReceived -= this.MessageReceivedAsync;
            this.Save();
        }

        private async Task MessageReceivedAsync(SocketMessage msg) {
            if (msg.Author == Bot.Instance.Client.CurrentUser)
                return;

            string unparsed = msg.Content;
            if (unparsed.StartsWith(this.RPrefix)) {
                string[] components = unparsed.Substring(this.RPrefix.Length).Split(' ');
                string name = components.First().ToLower();
                string[] args = Bot.ParseArgs(string.Join(" ", components.Skip(1))).ToArray();

                if (this.RememberedCommands.TryGetValue(name, out RCommand cmd)) {
                    await cmd.ExecuteAsync(msg, args);
                }
            }
        }

        public override bool Validate(SocketMessage msg, string[] args) {
            return args.Length >= 2;
        }

        public override async Task Execute(SocketMessage msg, string[] args) {
            bool lua = false;
            if (args.Length > 2 && args[0] == "-l" || args[1] == "-l") {
                lua = true;
                args = args.Where((elem, i) => i != (args[0] == "-l" ? 0 : 1)).ToArray();
            }
            string name = args[0];
            string contents = string.Join(" ", args.Skip(1));

            RCommand newCmd = new RCommand(name, contents, lua) {
                Author = msg.Author.Id,
                CreationDate = msg.CreatedAt
            };
            if (this.RememberedCommands.TryGetValue(name, out RCommand cmd)) {
                Task send = msg.Channel.SendMessageAsync($"{msg.Author.Mention} Replacing command {name}");
                this.RememberedCommands.AddOrUpdate(name, newCmd, (key, val) => newCmd);
                await send;
            } else {
                Task send = msg.Channel.SendMessageAsync($"{msg.Author.Mention} Creating command {name}");
                this.RememberedCommands.AddOrUpdate(name, newCmd, (key, val) => newCmd);
                await send;
            }
            this.Save();
        }

        public void Save() {
            if (!Directory.Exists(Directory.GetCurrentDirectory()))
                return;

            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "remember.json"), JsonConvert.SerializeObject(this.RememberedCommands, Formatting.Indented));
        }

        public void Load() {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "remember.json");
            if (!File.Exists(path))
                return;

            this.RememberedCommands = JsonConvert.DeserializeObject<ConcurrentDictionary<string, RCommand>>(File.ReadAllText(path));
        }

        #region Properties
        public override CommandDocs Documentation { get; }

        public ConcurrentDictionary<string, RCommand> RememberedCommands { get; set; } = new ConcurrentDictionary<string, RCommand>();

        public string RPrefix {
            get {
                Config config = Bot.Instance.Config;
                if (config.Strings.TryGetValue($"{this.ConfigNamespace}.prefix", out string prefix))
                    return prefix;
                prefix = config.Strings.GetOrAdd($"{this.ConfigNamespace}.prefix", "!");
                Bot.Instance.Save();
                return prefix;
            }
        }
        #endregion

        public class RCommand {
            public string Name { get; set; }
            public string Contents { get; set; }
            public bool IsLua { get; set; }

            public ulong? Author { get; set; }
            public DateTimeOffset? CreationDate { get; set; }

            public RCommand(string name, string contents, bool isLua = false) {
                this.Name = name;
                this.Contents = contents;
                this.IsLua = isLua;

                // TODO: Creator, creation time, etc
            }

            public async Task ExecuteAsync(SocketMessage msg, string[] args) {
                try {
                    using (Lua interpreter = Bot.Instance.GetInterpreter()) {
                        // Get reference to _G
                        // ReSharper disable once InconsistentNaming
                        LuaTable _G = interpreter.GetTable("_G");

                        // Execute command
                        if (!this.IsLua) {
                            string output = this.Contents;
                            //output = RCommand.VarMatcher.Replace(output, match => interpreter.DoString($"return (function(...) return {match.Value.Trim('%')} end)({string.Join(", ", args)})", Name).FirstOrDefault()?.ToString() ?? "nil");
                            await msg.Channel.SendMessageAsync(output);
                        } else {
                            LuaFunction f = interpreter.LoadString(this.Contents, this.Name);
                            f.Call(args.ToArray<object>());
                        }
                    }
                } catch (LuaException ex) {
                    await msg.Channel.SendMessageAsync($"```{ex.Message}\n{ex.StackTrace}```");
                } catch (Exception ex) {
                    await msg.Channel.SendMessageAsync($"```{ex.Message}\n{ex.StackTrace}```");
                }
            }

            public static Regex VarMatcher = new Regex("(?<=(^|[^%])(%%)*)%([^%]+)%(?=(%%)*([^%]|$))", RegexOptions.Compiled | RegexOptions.Multiline);

            static RCommand() {

            }
        }
    }
}
