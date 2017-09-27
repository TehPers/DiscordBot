using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace TehPers.Discord.TehBot.Commands {
    public abstract class Command {

        protected Command(string name) {
            name = name.ToLower();
            this.Name = name;
        }

        /// <summary>Unloads the command</summary>
        public virtual void Unload() { }

        /// <summary>Returns whether this command should be executed</summary>
        /// <param name="msg">The message sent on the server</param>
        /// <param name="args">The arguments to the command</param>
        /// <returns>Whether to execute the command</returns>
        public abstract bool Validate(SocketMessage msg, string[] args);

        /// <summary>Executes the command asynchronously. Make sure to call <seealso cref="Validate"/> first!</summary>
        /// <param name="msg">The message sent on the server</param>
        /// <param name="args">The arguments to the command</param>
        public abstract Task Execute(SocketMessage msg, string[] args);

        /// <summary>Displays the command's documentation asynchronously</summary>
        /// <param name="channel">The channel to send the documentation to</param>
        public virtual async Task DisplayUsage(ISocketMessageChannel channel) {
            EmbedBuilder embed = new EmbedBuilder {
                Color = Color.Blue,
                Title = $"{Command.Prefix}{this.Name} {string.Join(" ", this.Documentation.Arguments.Select(ArgSelector))}",
                Description = this.Documentation.Description,
                Fields = this.Documentation.Arguments.Select(arg => new EmbedFieldBuilder {
                    IsInline = false,
                    Name = ArgSelector(arg),
                    Value = arg.Description
                }).ToList()
            };

            await channel.SendMessageAsync("", false, embed.Build());

            string ArgSelector(CommandDocs.Argument arg) => $"{(arg.Optional ? "[" : "<")}{arg.Name}{(arg.Optional ? "]" : ">")}";
        }

        /// <summary>Reloads every command</summary>
        [SuppressMessage("ReSharper", "ObjectCreationAsStatement")]
        public static void ReloadCommands() {
            Bot.Instance.Log(new LogMessage(LogSeverity.Verbose, "BOT", "Loading commands..."));

            // Clear commands list
            foreach (Command c in Command.CommandList.Values)
                c.Unload();

            Command.CommandList.Clear();

            // Load new commands
            Command.AddCommand(new HelpCommand("help"));
            Command.AddCommand(new GameStatsCommand("stats", "heroes"));
            Command.AddCommand(new GameStatsCommand("skills", "skills"));
            Command.AddCommand(new ConfigCommand("config"));
            Command.AddCommand(new RememberCommand("r"));
            Command.AddCommand(new ForgetCommand("f"));
            Command.AddCommand(new ReloadCommand("reload"));
            Command.AddCommand(new PermissionsCommand("roles"));
            Command.AddCommand(new ExecuteCommand("exec"));

            // Save any changes to the config
            Bot.Instance.Save();

            Bot.Instance.Log(new LogMessage(LogSeverity.Verbose, "BOT", "Loaded commands"));
        }

        /// <summary>Adds a command to the bot</summary>
        /// <param name="cmd">The command to add to the bot</param>
        public static void AddCommand(Command cmd) {
            if (Bot.Instance.Config.Bools.GetOrAdd(cmd.ConfigNamespace, true))
                Command.CommandList.GetOrAdd(cmd.Name, cmd);
            else
                cmd.Unload();
        }

        /// <summary>This command's name</summary>
        public string Name { get; }

        /// <summary>This command's documentation</summary>
        public abstract CommandDocs Documentation { get; }

        /// <summary>The namespace for this command in the configs and permissions</summary>
        public string ConfigNamespace => $"command.{this.Name}";

        /// <summary>Whether users should be able to use this by default</summary>
        public virtual bool DefaultPermission => false;

        /// <summary>A dictionary mapping every loaded command to its name</summary>
        public static ConcurrentDictionary<string, Command> CommandList { get; } = new ConcurrentDictionary<string, Command>();

        /// <summary>The prefix required to indicate that a message is a command</summary>
        public static string Prefix {
            get {
                Config config = Bot.Instance.Config;
                if (config.Strings.TryGetValue("command.prefix", out string prefix))
                    return prefix;
                prefix = config.Strings.GetOrAdd("command.prefix", ".");
                Bot.Instance.Save();
                return prefix;
            }
        }
    }
}
