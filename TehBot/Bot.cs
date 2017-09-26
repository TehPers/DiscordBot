using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using NLua;
using TehPers.Discord.TehBot.Commands;
using TehPers.Discord.TehBot.Permissions;
using TehPers.Discord.TehBot.Permissions.Tables;
using TehPers.Discord.TehBot.Properties;
using static TehPers.Discord.TehBot.Config;

namespace TehPers.Discord.TehBot {
    public class Bot : IDisposable {

        public static Bot Instance { get; private set; }

        public DiscordSocketClient Client { get; }

        public Config Config { get; set; }

        public BotDatabase Database { get; set; }

        public PermissionHandler Permissions { get; }

        public Bot() {
            if (Bot.Instance != null)
                return;
            Bot.Instance = this;

            this.Permissions = new PermissionHandler();

            string dbPath = Path.Combine(Directory.GetCurrentDirectory(), "data.db");
            bool newDb = false;
            if (!File.Exists(dbPath)) {
                this.Log($"Created database at {dbPath}");
                SQLiteConnection.CreateFile(dbPath);
                newDb = true;
            }

            DbConnection connection = new SQLiteConnection($"Data Source={dbPath};");
            connection.Open();
            this.Database = new BotDatabase(connection);

            if (newDb) {
                this.Database.CreateSchema();
                this.Database.SaveChanges();
            }

            this.Client = new DiscordSocketClient();
            this.Client.Log += this.LogAsync;
            this.Client.MessageReceived += this.MessageReceivedAsync;
            this.Client.Ready += this.ReadyAsync;

            this.AfterLoaded += async (sender, e) => {
                List<Task> tasks = new List<Task>();

                Command.ReloadCommands();

                if (this.Config.Strings.TryGetValue("bot.username", out string name) && this.Client.CurrentUser.Username != name) {
                    this.Log(new LogMessage(LogSeverity.Verbose, "LOG", $"Setting username to {name}"));
                    tasks.Add(this.Client.CurrentUser.ModifyAsync(properties => properties.Username = name));
                }

                if (this.Config.Strings.TryGetValue("bot.game", out string game) && this.Client.CurrentUser.Game?.Name != game) {
                    this.Log(new LogMessage(LogSeverity.Verbose, "LOG", $"Setting game to {game}"));
                    string format = string.Format(game, Command.Prefix);
                    tasks.Add(this.Client.SetGameAsync(format));
                }

                /*if (Config.Strings.TryGetValue("bot.avatar", out string picture)) {
                    Log(new LogMessage(LogSeverity.Verbose, "LOG", $"Setting avatar to {picture}"));
                    string path = Path.Combine(Directory.GetCurrentDirectory(), picture);

                    if (File.Exists(path)) {
                        // Just get this one over with
                        try {
                            await Client.CurrentUser.ModifyAsync(settings => settings.Avatar = new Image(path));
                            File.Move(path, $"{path}.loaded");
                        } catch (IOException) {
                            Log(new LogMessage(LogSeverity.Verbose, "LOG", $"Failed to set avatar"));
                        }
                    }
                }*/

                if (newDb) {
                    // Create roles
                    tasks.Add(this.Permissions.CreateRoleAsync(null, "Global Administrator"));
                    tasks.Add(this.Permissions.CreateRoleAsync(null, "Default"));
                    await Task.WhenAll(tasks.ToArray());
                    tasks.Clear();

                    // Create permissions
                    tasks.Add(this.Permissions.GivePermissionAsync(null, "Global Administrator", "*"));
                    tasks.AddRange(from command in Command.CommandList.Values
                                   where command.DefaultPermission
                                   select this.Permissions.GivePermissionAsync(null, "Default", command.ConfigNamespace));

                    // Create role assignments
                    tasks.Add(this.Permissions.AssignRoleAsync(null, "Default", null));
                    tasks.Add(this.Permissions.AssignRoleAsync(null, "Global Administrator", 247080708454088705UL));
                }

                await Task.WhenAll(tasks);
            };
        }

        public async Task<bool> StartAsync() {
            this.LoadOnly();

            string token = this.Config.Secrets?.Token;
            if (string.IsNullOrEmpty(token)) {
                this.Log($"Invalid Discord token: {token ?? "(null)"}", LogSeverity.Critical);
                return false;
            }

            await this.Client.LoginAsync(TokenType.Bot, token);

            await this.Client.StartAsync();

            return true;
        }

        public void Save() {
            if (!Directory.Exists(Directory.GetCurrentDirectory()))
                return;

            string path = Path.Combine(Directory.GetCurrentDirectory(), "config.json");
            this.Log(new LogMessage(LogSeverity.Verbose, "BOT", "Saving config"));
            File.WriteAllText(path, JsonConvert.SerializeObject(this.Config, Formatting.Indented));

            this.OnAfterSaved();
        }

        private void LoadOnly() {
            this.Log(new LogMessage(LogSeverity.Verbose, "BOT", "Loading config"));

            string config = Path.Combine(Directory.GetCurrentDirectory(), "config.json");
            string secrets = Path.Combine(Directory.GetCurrentDirectory(), "Secret");

            if (File.Exists(config)) {
                this.Config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(config));
            } else {
                this.Log(new LogMessage(LogSeverity.Verbose, "BOT", "Config not found, creating new config"));
                this.Config = new Config();
                this.Save();
            }

            config = Path.Combine(secrets, "bot.json");
            if (File.Exists(config)) {
                this.Config.Secrets = JsonConvert.DeserializeObject<SecretConfigs>(File.ReadAllText(config));
            } else {
                Directory.CreateDirectory(secrets);
                this.Log(new LogMessage(LogSeverity.Verbose, "BOT", "Secret config not found, creating new config"));
                this.Config = new Config();
                this.Save();
            }
        }

        public void Load() {
            this.LoadOnly();
            this.OnAfterLoaded();
        }

        #region Handlers
        public async Task MessageReceivedAsync(SocketMessage msg) {
            if (msg.Author == Bot.Instance.Client.CurrentUser)
                return;

            if (msg.Content.StartsWith(Command.Prefix)) {
                await this.CommandHandlerAsync(msg);
            }
        }

        public Task LogAsync(string message, LogSeverity severity = LogSeverity.Info, string source = "BOT", Exception exception = null) => this.LogAsync(new LogMessage(severity, source, message, exception));
        public Task LogAsync(LogMessage msg) {
            this.Log(msg);
            return Task.CompletedTask;
        }

        public void Log(string message, LogSeverity severity = LogSeverity.Info, string source = "BOT", Exception exception = null) => this.Log(new LogMessage(severity, source, message, exception));
        public void Log(LogMessage msg) => Console.WriteLine(msg.ToString());

        private Task ReadyAsync() {
            this.Load();
            return Task.CompletedTask;
        }
        #endregion

        public async Task CommandHandlerAsync(SocketMessage msg) {
            string[] components = msg.Content.Substring(Command.Prefix.Length).FixPunctuation().Split(' ');
            string cmd = components.First().ToLower();

            // Parse arguments
            string[] args = Bot.ParseArgs(string.Join(" ", components.Skip(1))).ToArray();
            if (Command.CommandList.TryGetValue(cmd, out Command command) && await this.Permissions.HasPermissionAsync(msg.GetGuild().Id, msg.Author.Id, command.ConfigNamespace)) {
                this.Log(new LogMessage(LogSeverity.Verbose, "LOG", $"[#{msg.Channel.Name}] {msg.Author.Discriminator}: {msg.Content}"));

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

        public Lua GetInterpreter() => this.GetInterpreter(null);
        public Lua GetInterpreter(Action<object> print, uint maxPrints = 1) {
            Lua interpreter = new Lua();

            string Time(string format) => format == null ? DateTime.Now.ToString(CultureInfo.CurrentCulture) : DateTime.Now.ToString(format, CultureInfo.CurrentCulture);

            interpreter.LoadString(Resources.InterpreterInit, "init").Call((Func<string, string>) Time, print);

            return interpreter;
        }

        public void Dispose() {
            this.Client?.Dispose();
            this.Database.Dispose();
        }

        #region Events
        public event EventHandler AfterLoaded;
        protected virtual void OnAfterLoaded() => this.AfterLoaded?.Invoke(this, EventArgs.Empty);

        public event EventHandler AfterSaved;
        protected virtual void OnAfterSaved() => this.AfterSaved?.Invoke(this, EventArgs.Empty);
        #endregion
    }
}
