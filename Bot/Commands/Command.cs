using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Discord;

namespace Bot.Commands {
    public abstract class Command {
        public string Name { get; }
        protected CommandUsage Usage { get; }
        private readonly HashSet<Type> _verbs = new HashSet<Type>();
        private Func<IUserMessage, string[], Task> _parseOptions;
        protected Parser CommandParser { get; } = new Parser(settings => {
            settings.CaseInsensitiveEnumValues = true;
            settings.CaseSensitive = false;
            settings.HelpWriter = TextWriter.Null;
        });

        protected Command(string name) {
            this.Name = name;
            this.Usage = new CommandUsage(this);
        }

        /// <summary>Called when the command is loaded</summary>
        public virtual Task Load() => Task.CompletedTask;

        /// <summary>Called when the command is unloaded</summary>
        public virtual Task Unload() => Task.CompletedTask;

        /// <summary>Called when the bot saves</summary>
        public virtual void Save(object sender, EventArgs eventArgs) { }

        /// <summary>Execute the command with the given arguments</summary>
        /// <param name="message">The message which executed this command</param>
        /// <param name="args">The arguments being passed to this message</param>
        /// <remarks>This automatically calls <see cref="Verb.Execute"/> for the appropriate verb</remarks>
        public virtual Task Execute(IUserMessage message, IEnumerable<string> args) {
            string[] argsArray = args as string[] ?? args.ToArray();

            if (this._parseOptions == null) {
                Task task = Task.CompletedTask;
                ParserResult<object> result = this.CommandParser.ParseArguments(argsArray, this._verbs.ToArray());
                result.WithParsed<Verb>(verb => task = verb.Execute(this, message, argsArray))
                    .WithNotParsed(errors => task = this.ShowHelp(message, argsArray, errors));
                return task;
            } else {
                return this._parseOptions(message, argsArray);
            }
        }

        /// <summary>Replies to the message with the command's usage information</summary>
        /// <param name="message">The message to reply to</param>
        /// <param name="args">The args passed to the command</param>
        /// <param name="errors">The errors from parsing the command</param>
        public virtual Task ShowHelp(IMessage message, IEnumerable<string> args, IEnumerable<Error> errors) => this.ShowHelp(message, args);

        /// <summary>Replies to the message with the command's usage information</summary>
        /// <param name="message">The message to reply to</param>
        /// <param name="args">The args passed to the command</param>
        public virtual Task ShowHelp(IMessage message, IEnumerable<string> args) {
            return message.Channel.SendMessageAsync($"{message.Author.Mention}", embed: this.Usage.BuildHelp(message.Channel));
        }

        /// <summary>Returns whether this command is enabled in the given guild</summary>
        /// <param name="guild">The guild</param>
        /// <returns>Whether this command is enabled</returns>
        public bool IsEnabled(IGuild guild) {
            return this.GetConfig(guild).GetValue(c => c.Enabled) ?? this.IsDefaultEnabled;
        }

        /// <summary>Whether this command is enabled by default</summary>
        protected virtual bool IsDefaultEnabled { get; } = true;

        /// <summary>Returns the name of the command in the given guild</summary>
        /// <param name="guild">The guild</param>
        /// <returns>The name of the command</returns>
        public string GetName(IGuild guild) {
            return this.GetConfig(guild).GetValue(c => c.Alias) ?? this.Name;
        }

        /// <summary>Whether the given user has permission to use this command on the given guild</summary>
        /// <param name="guild">The guild</param>
        /// <param name="user">The user</param>
        /// <returns>Whether the user has permission</returns>
        public virtual Task<bool> HasPermission(IGuild guild, IUser user) => Task.FromResult(true);

        /// <summary>Whether the given user can use this command on the given guild</summary>
        /// <param name="guild">The guild</param>
        /// <returns>True if the user can use this command, false otherwise</returns>
        public Task<bool> CanUse(IGuild guild) => Task.FromResult(this.IsEnabled(guild));

        /// <summary>Whether the given user can use this command on the given guild</summary>
        /// <param name="guild">The guild</param>
        /// <param name="user">The user</param>
        /// <returns>True if the user can use this command, false otherwise</returns>
        public async Task<bool> CanUse(IGuild guild, IUser user) => this.IsEnabled(guild) && await this.HasPermission(guild, user).ConfigureAwait(false);

        /// <summary>Adds a verb to the command. If it is the only verb, it will be used implicitly.</summary>
        /// <typeparam name="T">The type of the verb</typeparam>
        /// <returns>This command</returns>
        protected Command AddVerb<T>() where T : Verb {
            Type verb = typeof(T);
            if (!this._verbs.Add(verb))
                return this;

            this.Usage.AddVerb<T>();

            if (this._verbs.Count > 1) {
                this._parseOptions = null;
            } else {
                this._parseOptions = (message, args) => {
                    Task task = Task.CompletedTask;
                    ParserResult<T> result = this.CommandParser.ParseArguments<T>(args);
                    result.WithParsed(options => task = options.Execute(this, message, args))
                        .WithNotParsed(errors => task = this.ShowHelp(message, args, errors));

                    return task;
                };
            }

            return this;
        }

        /// <summary>Sets the description of this command</summary>
        /// <param name="description">The description for this command's <see cref="Command.Usage"/></param>
        /// <returns>This command</returns>
        protected Command WithDescription(string description) {
            this.Usage.SetDescription(description);
            return this;
        }

        #region Configs
        public ConfigHandler.ConfigWrapper<CommandConfig> GetConfig() {
            return Bot.Instance.Config.GetOrCreate<CommandConfig>($"command.{this.Name}");
        }

        public ConfigHandler.ConfigWrapper<CommandConfig> GetConfig(IGuild guild) => this.GetConfig(guild.Id);
        public ConfigHandler.ConfigWrapper<CommandConfig> GetConfig(ulong guild) {
            return Bot.Instance.Config.GetOrCreate<CommandConfig>($"command.{this.Name}", guild);
        }

        public ConfigHandler.ConfigWrapper<T> GetConfig<T>(string name) where T : IConfig, new() {
            return Bot.Instance.Config.GetOrCreate<T>($"command.{this.Name}.{name}");
        }

        public ConfigHandler.ConfigWrapper<T> GetConfig<T>(string name, IGuild guild) where T : IConfig, new() => this.GetConfig<T>(name, guild.Id);
        public ConfigHandler.ConfigWrapper<T> GetConfig<T>(string name, ulong guild) where T : IConfig, new() {
            return Bot.Instance.Config.GetOrCreate<T>($"command.{this.Name}.{name}", guild);
        }

        public class CommandConfig : IConfig {
            public bool? Enabled { get; set; }
            public string Alias { get; set; }
        }
        #endregion

        public abstract class Verb {
            public abstract Task Execute(Command cmd, IUserMessage message, string[] args);

            public virtual bool IsEnabled(IMessage message) => this.IsEnabled(message.Channel, message.Author);
            public virtual bool IsEnabled(IChannel channel, IUser user) => true;
        }

        #region Static
        public static ConcurrentDictionary<string, Command> CommandRegistry { get; } = new ConcurrentDictionary<string, Command>();

        /// <summary>Registers a command in the bot</summary>
        /// <param name="cmd">The command to register</param>
        public static void RegisterCommand(Command cmd) {
            if (!Command.CommandRegistry.TryAdd(cmd.Name, cmd))
                throw new ArgumentException("A command with that name has already been registered", nameof(cmd));

            Bot.Instance.AfterSaved += cmd.Save;
        }

        /// <summary>Registers the default commands</summary>
        public static void RegisterCommands() {
            Command.RegisterCommand(new CommandHelp("help"));
            Command.RegisterCommand(new CommandAdmin("admin"));
            Command.RegisterCommand(new CommandReload("reload"));
            Command.RegisterCommand(new CommandWFInfo("wfinfo"));
            Command.RegisterCommand(new CommandFEH("stats", "heroes"));
            Command.RegisterCommand(new CommandFEH("skills", "skills"));
            Command.RegisterCommand(new CommandFEH("weapons", "weapons"));
            Command.RegisterCommand(new CommandFEH("seals", "seals"));
            Command.RegisterCommand(new CommandFEH("buildings", "buildings"));
        }

        /// <summary>Gets all the commands usable on the given guild by the given user</summary>
        /// <param name="guild">The guild</param>
        /// <param name="user">The user</param>
        /// <returns>The available commands</returns>
        public static async Task<IEnumerable<Command>> AvailableCommands(IGuild guild, IUser user) {
            Task<Command>[] tasks = Command.CommandRegistry.Values.Select(c => c.CanUse(guild, user).ContinueWith(t => t.Result ? c : null)).ToArray();
            Command[] cmds = await Task.WhenAll(tasks).ConfigureAwait(false);
            return cmds.Where(c => c != null);
        }

        /// <summary>Gets all the commands enabled on the given guild</summary>
        /// <param name="guild">The guild</param>
        /// <returns>The enabled commands</returns>
        public static async Task<IEnumerable<Command>> AvailableCommands(IGuild guild) {
            Task<Command>[] tasks = Command.CommandRegistry.Values.Select(c => c.CanUse(guild).ContinueWith(t => t.Result ? c : null)).ToArray();
            Command[] cmds = await Task.WhenAll(tasks).ConfigureAwait(false);
            return cmds.Where(c => c != null);
        }

        /// <summary>Gets all the commands</summary>
        /// <returns>All registered commands</returns>
        public static IEnumerable<Command> AvailableCommands() => Command.CommandRegistry.Values;

        /// <summary>Gets a command on the guild using its local name</summary>
        /// <param name="guild">The guild</param>
        /// <param name="name">The name of the command</param>
        /// <returns>The command if found, else null</returns>
        public static Command GetCommand(IGuild guild, string name) => Command.CommandRegistry.Values.FirstOrDefault(c => string.Equals(c.GetName(guild), name, StringComparison.OrdinalIgnoreCase));

        /// <summary>Returns the prefix for the given guild, or the global one if not set</summary>
        /// <param name="guild">The guild</param>
        /// <returns>The prefix on the given guild, or global prefix if none set</returns>
        public static string GetPrefix(IGuild guild) => Bot.Instance.GetMainConfig(guild).GetValue(c => c.Prefix) ?? "!";
        #endregion
    }
}
