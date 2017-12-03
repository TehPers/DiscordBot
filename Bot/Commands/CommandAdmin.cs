using System;
using System.IO;
using CommandLine;
using Discord;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Bot.Helpers;

namespace Bot.Commands {
    public class CommandAdmin : Command {
        public CommandAdmin(string name) : base(name) {
            this.WithDescription("Extended control over the bot");
            this.AddVerb<CommandVerb>();
            this.AddVerb<PrefixVerb>();
            this.AddVerb<ListVerb>();
            this.AddVerb<NickVerb>();
            this.AddVerb<AvatarVerb>();
        }

        protected override bool IsDefaultEnabled { get; } = true;

        public override bool HasPermission(IGuild guild, IUser user) {
            return user.Id == 247080708454088705UL;
        }

        #region Verbs
        [Verb("command", HelpText = "Sets properties of a command")]
        public class CommandVerb : Verb {
            [Option('g', "global", Required = false, Default = false, HelpText = "Sets global command properties")]
            public bool Global { get; set; }

            [Option('a', "alias", Required = false, HelpText = "Sets the local name of the command")]
            public string Alias { get; set; }

            [Option('r', "remove-alias", Required = false, HelpText = "Removes the alias on the command")]
            public bool RemoveAlias { get; set; }

            [Option('e', "enable", Required = false, HelpText = "Enables the command")]
            public bool Enable { get; set; }

            [Option('d', "disable", Required = false, HelpText = "Disables the command")]
            public bool Disable { get; set; }

            [Value(0, Required = true, MetaName = "command", HelpText = "The local name of the command")]
            public string Cmd { get; set; }

            public override async Task Execute(Command adminCmd, IMessage message, string[] args) {
                IGuild guild = message.Channel.GetGuild();

                // Get the command
                Command cmd = Command.GetCommand(guild, this.Cmd);
                if (cmd == null) {
                    await message.Reply($"Unknown command '{this.Cmd}'").ConfigureAwait(false);
                    return;
                }

                // Modify the config
                ConfigHandler.ConfigWrapper<CommandConfig> config = this.Global ? cmd.GetConfig() : cmd.GetConfig(guild);
                config.SetValue(c => {
                    // Enabled
                    if (this.Disable != this.Enable) {
                        if (this.Enable)
                            c.Enabled = true;
                        if (this.Disable)
                            c.Enabled = false;
                    }

                    // Alias
                    if (this.RemoveAlias)
                        c.Alias = null;
                    if (this.Alias != null)
                        c.Alias = this.Alias;
                });

                await config.Save().ConfigureAwait(false);
                await message.Reply($"Command '{cmd.Name}' modified successfully").ConfigureAwait(false);
            }
        }

        [Verb("list", HelpText = "Lists all commands, including disabled ones")]
        public class ListVerb : Verb {
            public override Task Execute(Command cmd, IMessage message, string[] args) {
                return message.Reply(string.Join(", ", Command.AvailableCommands().Select(c => $"{c.GetName(message.GetGuild())} ({c.Name})")));
            }
        }

        [Verb("prefix", HelpText = "Sets or resets the command prefix")]
        public class PrefixVerb : Verb {
            [Option('g', "global", Required = false, Default = false, HelpText = "Sets global command properties")]
            public bool Global { get; set; }

            [Value(0, MetaName = "prefix", HelpText = "The new prefix of the command. Leave this out to reset the prefix")]
            public string Prefix { get; set; }

            public override async Task Execute(Command cmdAdmin, IMessage message, string[] args) {
                ConfigHandler.ConfigWrapper<Bot.MainConfig> config = this.Global ? Bot.Instance.GetMainConfig() : Bot.Instance.GetMainConfig(message.Channel.GetGuild());
                config.SetValue(c => c.Prefix = this.Prefix);
                await config.Save().ConfigureAwait(false);
                await message.Reply($"Command prefix set to '{this.Prefix}'{(this.Global ? " globally" : string.Empty)}").ConfigureAwait(false);
            }
        }

        [Verb("nick", HelpText = "Sets the bot's nickname on this guild")]
        public class NickVerb : Verb {
            [Value(0, MetaName = "nickname", Required = false, HelpText = "The new nickname for the bot. Leave empty to remove the nickname")]
            public string Nick { get; set; }

            public override async Task Execute(Command cmd, IMessage message, string[] args) {
                IGuildUser user = await message.GetGuild().GetCurrentUserAsync().ConfigureAwait(false);
                await user.ModifyAsync(properties => properties.Nickname = this.Nick).ConfigureAwait(false);
                if (this.Nick == null) {
                    await message.Reply("Nickname removed").ConfigureAwait(false);
                } else {
                    await message.Reply($"Nickname changed to '{this.Nick}'").ConfigureAwait(false);
                }
            }
        }

        [Verb("avatar", HelpText = "Sets the bot's avatar")]
        public class AvatarVerb : Verb {
            public override async Task Execute(Command cmd, IMessage message, string[] args) {
                IAttachment attachment = message.Attachments.FirstOrDefault();
                if (attachment == null) {
                    await message.Reply("You must attach an image to this command").ConfigureAwait(false);
                    return;
                }

                try {
                    string downloadPath = Path.GetTempFileName();

                    // Download the image
                    using (WebClient client = new WebClient())
                        await client.DownloadFileTaskAsync(attachment.Url, downloadPath).ConfigureAwait(false);

                    // Create the image file
                    Image image = new Image(downloadPath);

                    // Upload the image
                    await Bot.Instance.Client.CurrentUser.ModifyAsync(p => {
                        p.Avatar = image;
                    }).ConfigureAwait(false);
                } catch (Exception ex) {
                    Bot.Instance.Log("Error while updating avatar", LogSeverity.Error, exception: ex);
                    await message.Reply($"An error has occurred while downloading the image:```{ex.Message}```").ConfigureAwait(false);
                }
            }
        }

        [Verb("reset", HelpText = "Resets a config file")]
        public class ResetVerb : Verb {
            [Value(0, Required = false, MetaName = "config", HelpText = "The config to reset. If excluded, will reset every config.")]
            public string Config { get; set; }

            [Option('g', "global", Required = false, HelpText = "Modify the global config")]
            public bool Global { get; set; }

            public override Task Execute(Command cmd, IMessage message, string[] args) {
                // TODO
                return Task.CompletedTask;
            }
        }
        #endregion
    }
}
