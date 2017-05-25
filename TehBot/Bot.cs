using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using NLua;
using TehPers.Discord.TehBot.Commands;

namespace TehPers.Discord.TehBot {
    public class Bot : IDisposable {

        public static Bot Instance;

        public DiscordSocketClient Client { get; }

        public Config Config { get; set; }

        public Bot() {
            if (Bot.Instance != null)
                return;
            Bot.Instance = this;

            Client = new DiscordSocketClient();
            Client.Log += LogAsync;
            Client.MessageReceived += MessageReceivedAsync;
            Client.Ready += ReadyAsync;

            // Set up interpreter
            // ReSharper disable once InconsistentNaming
            LuaTable _G = Interpreter.GetTable("_G");
            _G["time"] = Interpreter.LoadString(
                "local time = ({...})[1]\n" +
                "return function(format)\n" +
                "  return time(format)\n" +
                "end",
                "Bot").Call(new Func<string, string>(Time)).FirstOrDefault() as LuaFunction;

            string Time(string format = null)
            {
                return format == null ? DateTime.Now.ToString(CultureInfo.CurrentCulture) : DateTime.Now.ToString(format, CultureInfo.CurrentCulture);
            }
        }

        public async Task LoginAsync(string token) => await Client.LoginAsync(TokenType.Bot, token);
        public async Task StartAsync() => await Client.StartAsync();

        public void Save() {
            if (!Directory.Exists(Directory.GetCurrentDirectory()))
                return;

            string path = Path.Combine(Directory.GetCurrentDirectory(), "config.json");
            Log(new LogMessage(LogSeverity.Verbose, "BOT", "Saving config"));
            File.WriteAllText(path, JsonConvert.SerializeObject(Config, Formatting.Indented));
        }

        public void Load() {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "config.json");
            if (File.Exists(path)) {
                Log(new LogMessage(LogSeverity.Verbose, "BOT", "Loading config"));
                Config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(path));
            } else {
                Log(new LogMessage(LogSeverity.Verbose, "BOT", "Config not found, creating new config"));
                Config = new Config();
                Save();
            }
        }

        #region Events
        public async Task MessageReceivedAsync(SocketMessage msg) {
            if (msg.Author == Bot.Instance.Client.CurrentUser)
                return;

            if (msg.Content.StartsWith(Command.Prefix)) {
                Log(new LogMessage(LogSeverity.Verbose, $"#{msg.Channel.Name}", msg.Content));
                await CommandHandlerAsync(msg);
            }
        }

        public Task LogAsync(LogMessage msg) {
            Log(msg);
            return Task.CompletedTask;
        }

        public void Log(LogMessage msg) {
            Console.WriteLine(msg.ToString());
        }

        private Task ReadyAsync() {
            Load();
            Command.ReloadCommands();

            return Task.CompletedTask;
        }
        #endregion

        public async Task CommandHandlerAsync(SocketMessage msg) {
            string[] components = msg.Content.Substring(Command.Prefix.Length).Split(' ');
            string cmd = components.First().ToLower();

            // Parse arguments
            string[] args = Bot.ParseArgs(string.Join(" ", components.Skip(1))).ToArray();
            if (Command.CommandList.TryGetValue(cmd, out Command command)) {
                if (command.Validate(msg, args))
                    await command.Execute(msg, args);
                else {
                    await command.DisplayUsage(msg.Channel);
                }
            }
        }

        public static IEnumerable<string> ParseArgs(string rawArgs) {
            List<string> argsList = new List<string>();
            bool escaped = false;
            bool quoted = false;
            string curArg = "";
            foreach (char c in rawArgs) {
                if (curArg == "" && c == '"') {
                    // Check if this arg is quoted
                    quoted = true;
                } else if (escaped) {
                    // Treat character as a literal
                    curArg += c;
                    escaped = false;
                } else if (c == '\\') {
                    // Escape next character
                    escaped = true;
                } else if (!quoted && char.IsWhiteSpace(c)) {
                    // End of argument
                    if (string.IsNullOrWhiteSpace(curArg))
                        continue;

                    argsList.Add(curArg);
                    curArg = "";
                } else if (quoted && c == '"') {
                    // End of quote
                    quoted = false;
                } else {
                    curArg += c;
                }
            }

            // Last argument
            if (quoted)
                curArg = $"\"{curArg}";
            if (escaped)
                curArg += "\\";
            if (curArg != "") {
                argsList.Add(curArg);
            }

            return argsList;
        }

        public Lua Interpreter { get; } = new Lua();

        public void Dispose() {
            Client?.Dispose();
            Interpreter?.Dispose();
        }
    }
}
