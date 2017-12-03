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
using Bot.Helpers;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace Bot {
    public class Bot : IDisposable {
        public static Bot Instance { get; private set; }
        public static string RootDirectory { get; set; } = Directory.GetCurrentDirectory();
        public static string ConfigsPath { get; } = Path.Combine(Bot.RootDirectory, "Configs");
        public static string MainConfigPath { get; } = Path.Combine(Bot.RootDirectory, "Secret", "bot.json");
        private static string LogPath { get; } = Path.Combine(Bot.RootDirectory, "Logs");
        private static FileStream LogStream { get; set; }

        public DiscordSocketClient Client { get; }
        public ConfigHandler Config { get; }
        public Timer SecondsTimer { get; } = new Timer(1000);

        public Bot() {
            if (Bot.Instance != null)
                return;

            Bot.Instance = this;
            this.Config = new ConfigHandler(Bot.ConfigsPath);

            // Setup bot client
            this.Client = new DiscordSocketClient();
            this.Client.Log += this.LogAsync;
            this.Client.MessageReceived += this.MessageReceivedAsync;
            this.Client.Ready += this.ReadyAsync;
            this.AfterLoaded += this.AfterFirstLoad;
            this.Logged += (bot, msg) => {
                Console.ResetColor();
                string severity;
                switch (msg.Severity) {
                    case LogSeverity.Critical:
                        Console.BackgroundColor = ConsoleColor.White;
                        Console.ForegroundColor = ConsoleColor.Red;
                        severity = "CRITICAL";
                        break;
                    case LogSeverity.Error:
                        Console.ForegroundColor = ConsoleColor.Red;
                        severity = "ERROR";
                        break;
                    case LogSeverity.Warning:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        severity = "WARNING";
                        break;
                    case LogSeverity.Info:
                        severity = "INFO";
                        break;
                    case LogSeverity.Verbose:
                        severity = "VERBOSE";
                        break;
                    case LogSeverity.Debug:
                        severity = "DEBUG";
                        break;
                    default:
                        severity = "UNKNOWN";
                        break;
                }

                Console.WriteLine($"[{severity}] {msg.ToString()}");
            };

            // Setup log file
            Directory.CreateDirectory(Path.GetDirectoryName(Bot.LogPath));
            Bot.LogStream = File.OpenWrite(Bot.LogPath);
            this.Logged += (bot, msg) => {
                byte[] data = Encoding.UTF8.GetBytes(msg.ToString());
                Bot.LogStream.Write(data, 0, data.Length);
            };
        }

        private async void AfterFirstLoad(object sender, EventArgs args) {
            // Register commands
            Command.RegisterCommands();

            // Load all commands
            foreach (Command cmd in Command.CommandRegistry.Values)
                await cmd.Load().ConfigureAwait(false);

            // Start ticking
            this.SecondsTimer.Start();

            // Don't run this function again
            this.AfterLoaded -= this.AfterFirstLoad;
        }

        public async Task<bool> StartAsync() {
            // Load main config
            BotConfig mainConfig;
            try {
                if (!File.Exists(Bot.MainConfigPath)) {
                    mainConfig = new BotConfig();
                    await File.WriteAllTextAsync(Bot.MainConfigPath, JsonConvert.SerializeObject(mainConfig)).ConfigureAwait(false);
                    this.Log($"Bot config file not found. Creating new one at {Bot.MainConfigPath}. Please fill it in.");
                    return false;
                }

                using (StreamReader file = File.OpenText(Bot.MainConfigPath)) {
                    mainConfig = JsonConvert.DeserializeObject<BotConfig>(await file.ReadToEndAsync().ConfigureAwait(false));
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

            await this.Client.LoginAsync(TokenType.Bot, token).ConfigureAwait(false);
            await this.Client.StartAsync().ConfigureAwait(false);
            return true;
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
                    await this.CommandHandlerAsync(msg).ConfigureAwait(false);
                }
            } catch (Exception ex) {
                this.Log("An error occured while handing a message", LogSeverity.Error, exception: ex);
            }
        }

        public Task LogAsync(string message) => this.LogAsync(message, LogSeverity.Info, null, null);
        public Task LogAsync(string message, LogSeverity severity) => this.LogAsync(message, severity, null, null);
        public Task LogAsync(string message, LogSeverity severity, Exception exception) => this.LogAsync(message, severity, exception, null);
        public Task LogAsync(string message, LogSeverity severity, Exception exception, string source) => this.LogAsync(new LogMessage(severity, source, message, exception));
        public Task LogAsync(LogMessage msg) {
            this.Log(msg);
            return Task.CompletedTask;
        }

        public void Log(string message) => this.Log(message, LogSeverity.Info, null, null);
        public void Log(string message, LogSeverity severity) => this.Log(message, severity, null, null);
        public void Log(string message, LogSeverity severity, Exception exception) => this.Log(message, severity, exception, null);
        public void Log(string message, LogSeverity severity, Exception exception, string source) => this.Log(new LogMessage(severity, source, message, exception));
        public void Log(LogMessage msg) => this.OnLogged(msg);

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
                } else {
                    switch (c) {
                        case '\\':
                            escaped = true;
                            break;
                        case '"' when quoted:
                            quoted = false;
                            done = true;
                            break;
                        case '"' when builder.Length == 0:
                            quoted = true;
                            break;
                        case ' ' when !quoted:
                            if (string.IsNullOrWhiteSpace(builder.ToString())) {
                                builder.Clear();
                            } else {
                                done = true;
                            }
                            break;
                        default:
                            builder.Append(c);
                            break;
                    }
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
                this.Log($"Failed to parse message: {msg.Content}", LogSeverity.Warning);
                return;
            }

            Command cmd = Command.AvailableCommands(msg.Channel.GetGuild(), msg.Author).FirstOrDefault(c => c.GetName(msg.Channel.GetGuild()) == cmdName);
            if (cmd != null) {
                await cmd.Execute(msg, args).ConfigureAwait(false);
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

        public delegate void LoggedEvent(Bot sender, LogMessage message);
        public event LoggedEvent Logged;
        protected virtual void OnLogged(LogMessage message) => this.Logged?.Invoke(this, message);
        #endregion

        #region Helpers
        public SocketChannel GetChannel(ulong id) => this.Client.GetChannel(id);

        public ConfigHandler.ConfigWrapper<MainConfig> GetMainConfig() => this.Config.GetOrCreate<MainConfig>("bot");
        public ConfigHandler.ConfigWrapper<MainConfig> GetMainConfig(IGuild guild) => this.Config.GetOrCreate<MainConfig>("bot", guild);
        public ConfigHandler.ConfigWrapper<MainConfig> GetMainConfig(ulong guild) => this.Config.GetOrCreate<MainConfig>("bot", guild);
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
