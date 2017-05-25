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
            Name = name;
        }

        public virtual void Unload() { }

        public abstract bool Validate(SocketMessage msg, string[] args);

        public abstract Task Execute(SocketMessage msg, string[] args);

        public async Task DisplayUsage(ISocketMessageChannel channel) {
            StringBuilder usage = new StringBuilder();
            usage.AppendLine($"**Usage**: {Command.Prefix}{Name} {string.Join(" ", Documentation.Arguments.Select(ArgSelector))}");
            usage.AppendLine($"**Description**: *{Documentation.Description}*");
            foreach (CommandDocs.Argument arg in Documentation.Arguments.OrderBy(arg => arg.Optional)) {
                usage.AppendLine($"**{arg.Name}** : {arg.Description}");
            }

            await channel.SendMessageAsync(usage.ToString());

            string ArgSelector(CommandDocs.Argument arg) => $"{(arg.Optional ? "[" : "<")}{arg.Name}{(arg.Optional ? "]" : ">")}";
        }

        [SuppressMessage("ReSharper", "ObjectCreationAsStatement")]
        public static void ReloadCommands() {
            Bot.Instance.Log(new LogMessage(LogSeverity.Verbose, "BOT", "Loading commands..."));

            // Clear commands list
            foreach (Command c in Command.CommandList.Values)
                c.Unload();

            Command.CommandList.Clear();

            // Load new commands
            Command.AddCommand(new HelpCommand("help"));
            Command.AddCommand(new StatsCommand("stats"));
            Command.AddCommand(new ConfigCommand("config"));
            Command.AddCommand(new RememberCommand("r"));
            Command.AddCommand(new ForgetCommand("f"));
            Command.AddCommand(new ReloadCommand("reload"));

            // Save any changes to the config
            Bot.Instance.Save();

            Bot.Instance.Log(new LogMessage(LogSeverity.Verbose, "BOT", "Loaded commands"));
        }

        public static void AddCommand(Command cmd) {
            if (Bot.Instance.Config.Bools.GetOrAdd(cmd.ConfigNamespace, true))
                Command.CommandList.GetOrAdd(cmd.Name, cmd);
            else
                cmd.Unload();
        }

        public string Name { get; }
        public abstract CommandDocs Documentation { get; }
        public string ConfigNamespace => $"command.{Name}";

        public static ConcurrentDictionary<string, Command> CommandList { get; } = new ConcurrentDictionary<string, Command>();

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
