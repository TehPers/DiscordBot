using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using Bot.Commands;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace Bot {
    public class Bot : IDisposable {
        public static Bot Instance { get; private set; }
        public static string ConfigsPath = Path.Combine(Directory.GetCurrentDirectory(), "Configs");
        public static string MainConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "Secret", "bot.json");

        public DiscordSocketClient Client { get; }
        public ConfigHandler Config { get; }
        public Timer SecondsTimer { get; } = new Timer(1000);

        public Bot() {
            if (Bot.Instance != null)
                return;

            Bot.Instance = this;
            this.Config = new ConfigHandler(Bot.ConfigsPath);

            // Setup timer
            short seconds = 0;
            this.SecondsTimer.Elapsed += (sender, args) => {
                if (++seconds % 60 != 0)
                    return;

                // Save every minute
                this.Save();
                seconds = 0;
            };

            // Setup bot client
            this.Client = new DiscordSocketClient();
            this.Client.Log += this.LogAsync;
            this.Client.MessageReceived += this.MessageReceivedAsync;
            this.Client.Ready += this.ReadyAsync;
            this.AfterLoaded += this.AfterFirstLoad;
        }

        private async void AfterFirstLoad(object sender, EventArgs args) {
            // Register commands
            Command.RegisterCommands();

            // Load all commands
            foreach (Command cmd in Command.CommandRegistry.Values)
                await cmd.Load();

            // Start ticking
            this.SecondsTimer.Start();

            // Don't run this function again
            this.AfterLoaded -= this.AfterFirstLoad;
        }

        public async Task<bool> StartAsync() {
            // Load main config
            BotConfig mainConfig = null;
            try {
                if (!File.Exists(Bot.MainConfigPath)) {
                    mainConfig = new BotConfig();
                    await File.WriteAllTextAsync(Bot.MainConfigPath, JsonConvert.SerializeObject(mainConfig));
                    this.Log($"Bot config file not found. Creating new one at {Bot.MainConfigPath}. Please fill it in.");
                    return false;
                }

                using (StreamReader file = File.OpenText(Bot.MainConfigPath)) {
                    mainConfig = JsonConvert.DeserializeObject<BotConfig>(await file.ReadToEndAsync());
                }
            } catch (Exception ex) {
                this.Log("Failed to deserialize main config file: " + Bot.MainConfigPath, LogSeverity.Error, exception: ex);
                return false;
            }

            string token = mainConfig?.Token;
            if (string.IsNullOrEmpty(token)) {
                this.Log($"Invalid Discord token: {token ?? "(null)"}", LogSeverity.Critical);
                return false;
            }

            await this.Client.LoginAsync(TokenType.Bot, token);
            await this.Client.StartAsync();
            return true;
        }

        public void Save() {
            this.OnBeforeSaved();
            this.Config.Save();
            this.OnAfterSaved();
        }

        public void Load() {
            this.Config.Load();
            this.OnAfterLoaded();
        }

        #region Handlers
        public async Task MessageReceivedAsync(SocketMessage msg) {
            if (msg.Author.IsBot || msg.Channel.GetGuild() == null)
                return;

            try {
                string prefix = msg.GetPrefix();
                if (msg.Content.StartsWith(prefix)) {
                    await this.CommandHandlerAsync(msg);
                }
            } catch (Exception ex) {
                this.Log("An error occured while handing a message", LogSeverity.Error, exception: ex);
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

        public async Task CommandHandlerAsync(SocketMessage msg) {
            string rawCmd = msg.Content.Substring(msg.GetPrefix().Length).FixPunctuation();
            bool failed = false;

            // Parse command
            string cmdName = null;
            List<string> args = new List<string>();
            StringBuilder builder = new StringBuilder();
            bool quoted = false;
            bool escaped = false;
            foreach (char c in rawCmd) {
                bool done = false;

                // Handle current character
                if (escaped) {
                    builder.Append(c);
                } else if (c == '\\') {
                    escaped = true;
                    builder.Append(c);
                } else if (c == '"' && builder.Length == 0) {
                    quoted = true;
                } else if (c == '"' && quoted) {
                    quoted = false;
                    done = true;
                } else if (quoted) {
                    builder.Append(c);
                } else if (c == ' ') {
                    if (string.IsNullOrWhiteSpace(builder.ToString())) {
                        builder.Clear();
                    } else {
                        done = true;
                    }
                } else {
                    builder.Append(c);
                }

                // Handle arg if done
                if (done) {
                    try {
                        // This can throw an error in some cases
                        string unescaped = Regex.Unescape(builder.ToString());

                        // Make sure the command name is set
                        if (cmdName == null) {
                            cmdName = unescaped;
                        } else {
                            args.Add(unescaped);
                        }

                        builder.Clear();
                    } catch (Exception) {
                        failed = true;
                        break;
                    }
                }
            }

            // Make sure to get the last arg if it's there
            if (builder.Length > 0 || quoted) {
                string lastArg = quoted ? $"\"{builder}" : builder.ToString();

                try {
                    // This can throw an error in some cases
                    string unescaped = Regex.Unescape(lastArg);

                    // Make sure the command name is set
                    if (cmdName == null) {
                        cmdName = unescaped;
                    } else {
                        args.Add(unescaped);
                    }
                } catch (Exception) {
                    failed = true;
                }
            }

            if (cmdName == null || failed) {
                await msg.Reply("Failed to parse command.");
                return;
            }

            Command cmd = Command.AvailableCommands(msg.Channel.GetGuild(), msg.Author).FirstOrDefault(c => c.GetName(msg.Channel.GetGuild()) == cmdName);
            if (cmd != null) {
                await cmd.Execute(msg, args);
            } else {
                await msg.Reply($"Unknown command '{cmdName}'");
            }
        }
        #endregion

        #region Events
        public event EventHandler AfterLoaded;
        protected virtual void OnAfterLoaded() => this.AfterLoaded?.Invoke(this, EventArgs.Empty);

        public event EventHandler BeforeSaved;
        protected virtual void OnBeforeSaved() => this.BeforeSaved?.Invoke(this, EventArgs.Empty);

        public event EventHandler AfterSaved;
        protected virtual void OnAfterSaved() => this.AfterSaved?.Invoke(this, EventArgs.Empty);
        #endregion

        #region Helpers
        public SocketChannel GetChannel(ulong id) => this.Client.GetChannel(id);

        public ConfigHandler.ConfigWrapper<MainConfig> GetMainConfig() => Bot.Instance.Config.GetOrCreate<MainConfig>("bot");
        public ConfigHandler.ConfigWrapper<MainConfig> GetMainConfig(IGuild server) => Bot.Instance.Config.GetOrCreate<MainConfig>("bot", server);
        public ConfigHandler.ConfigWrapper<MainConfig> GetMainConfig(ulong server) => Bot.Instance.Config.GetOrCreate<MainConfig>("bot", server);
        #endregion

        public class MainConfig : IConfig {
            public string Prefix { get; set; }
        }

        private class BotConfig {
            public string Token { get; set; }
        }

        public void Dispose() {
            this.Client?.Dispose();
            this.SecondsTimer?.Dispose();
        }
    }
}
