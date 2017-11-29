using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Bot.Extensions;
using CommandLine;
using Discord;

namespace Bot.Commands {
    public class CommandUsage {
        private readonly Command _command;

        private readonly HashSet<Type> _verbs = new HashSet<Type>();

        public string Description { get; private set; }

        public CommandUsage(Command cmd) {
            this._command = cmd;
        }

        public CommandUsage AddVerb<T>() {
            this._verbs.Add(typeof(T));
            return this;
        }

        public CommandUsage SetDescription(string description) {
            this.Description = description;
            return this;
        }

        public Embed BuildHelp(IChannel channel) => this.BuildHelp(channel.GetGuild());
        public Embed BuildHelp(IGuild server) => this.BuildHelp(Command.GetPrefix(server), this._command.GetName(server));
        public Embed BuildHelp(string prefix, string commandName) {
            EmbedBuilder embed = new EmbedBuilder();
            embed.WithTitle($"Command Help: {prefix}{commandName}");
            if (this.Description != null)
                embed.WithDescription(this.Description);

            if (this._verbs.Count == 1) {
                StringBuilder nameBuilder = new StringBuilder($"{prefix}{commandName}");
                StringBuilder textBuilder = new StringBuilder();

                PropertyInfo[] properties = this._verbs.First().GetProperties(BindingFlags.Instance | BindingFlags.Public);

                // Add positional arguments
                var positional = from property in properties
                                 let attribute = property.GetCustomAttribute<ValueAttribute>()
                                 where attribute != null
                                 orderby attribute.Index
                                 select new { property, attribute };
                foreach (var property in positional)
                    this.BuildValueHelp(property.property, property.attribute, nameBuilder, textBuilder);

                // Add other options
                var named = from property in properties
                            let attribute = property.GetCustomAttribute<OptionAttribute>()
                            where attribute != null
                            orderby property.PropertyType == typeof(bool) ? (attribute.ShortName == string.Empty ? 1 : 2) : 0 descending
                            select new { property, attribute };
                foreach (var property in named)
                    this.BuildOptionHelp(property.property, property.attribute, nameBuilder, textBuilder);

                embed.AddField(nameBuilder.ToString(), textBuilder.ToString());
            } else if (this._verbs.Any()) {
                foreach (Type verb in this._verbs) {
                    EmbedFieldBuilder field = this.BuildVerbHelp(prefix, commandName, verb);
                    if (field != null) {
                        embed.AddField(field);
                    }
                }
            } else {
                throw new Exception("No verbs or options set");
            }

            return embed.Build();
        }

        private EmbedFieldBuilder BuildVerbHelp(string prefix, string commandName, Type verb) {
            VerbAttribute verbAttribute = verb.GetCustomAttribute<VerbAttribute>();
            if (verbAttribute == null || verbAttribute.Hidden)
                return null;

            StringBuilder nameBuilder = new StringBuilder($"{prefix}{commandName} {verbAttribute.Name}");
            StringBuilder textBuilder = new StringBuilder();
            textBuilder.AppendLine($"_{verbAttribute.HelpText}_");

            PropertyInfo[] properties = verb.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            // Add positional arguments
            var positional = from property in properties
                             let attribute = property.GetCustomAttribute<ValueAttribute>()
                             where attribute != null
                             orderby attribute.Index
                             select new { property, attribute };
            foreach (var property in positional)
                this.BuildValueHelp(property.property, property.attribute, nameBuilder, textBuilder);

            // Add other options
            var named = from property in properties
                        let attribute = property.GetCustomAttribute<OptionAttribute>()
                        where attribute != null
                        orderby property.PropertyType == typeof(bool) ? (attribute.ShortName == string.Empty ? 1 : 2) : 0 descending
                        select new { property, attribute };
            foreach (var property in named)
                this.BuildOptionHelp(property.property, property.attribute, nameBuilder, textBuilder);

            return new EmbedFieldBuilder {
                IsInline = false,
                Name = nameBuilder.ToString(),
                Value = textBuilder.ToString()
            };
        }

        private void BuildValueHelp(PropertyInfo property, ValueAttribute attribute, StringBuilder nameBuilder, StringBuilder textBuilder) {
            if (attribute == null || attribute.Hidden)
                return;

            nameBuilder.Append(" ");
            nameBuilder.Append(attribute.Required ? '<' : '[');

            // Option name
            string name = attribute.MetaName ?? property.Name;
            nameBuilder.Append($"{name} : {property.PropertyType.Name}");
            textBuilder.Append($"**{name}**");

            // Default value
            if (attribute.Default != null)
                textBuilder.Append($" = {attribute.Default}");
            textBuilder.AppendLine();

            // Option help text
            if (attribute.HelpText != null)
                textBuilder.AppendLine($"_{attribute.HelpText}_");

            // Min & Max
            bool min = attribute.Min >= 0;
            bool max = attribute.Max >= 0;
            if (min && max) {
                textBuilder.AppendLine($"Range: {attribute.Min} - {attribute.Max}");
            } else if (min) {
                textBuilder.AppendLine($"Min: {attribute.Min}");
            } else if (max) {
                textBuilder.AppendLine($"Max: {attribute.Max}");
            }

            nameBuilder.Append(attribute.Required ? '>' : ']');
        }

        private void BuildOptionHelp(PropertyInfo property, OptionAttribute attribute, StringBuilder nameBuilder, StringBuilder textBuilder) {
            if (attribute == null || attribute.Hidden)
                return;

            nameBuilder.Append(" ");
            if (!attribute.Required)
                nameBuilder.Append("[");

            // Option name
            nameBuilder.Append($"-{attribute.ShortName.IfEmpty($"-{attribute.LongName}")}");
            if (property.PropertyType != typeof(bool) && property.PropertyType != typeof(bool?))
                nameBuilder.Append($" <{attribute.LongName.IfEmpty(attribute.ShortName)} : {property.PropertyType.Name}>");

            textBuilder.Append("**");
            if (attribute.ShortName != string.Empty)
                textBuilder.Append($"-{attribute.ShortName}");
            if (attribute.ShortName != string.Empty)
                if (attribute.LongName != string.Empty)
                    textBuilder.Append(" | ");
            if (attribute.LongName != string.Empty)
                textBuilder.Append("--" + attribute.LongName);
            textBuilder.Append("**");

            // Default value
            if (attribute.Default != null)
                textBuilder.Append($" = {attribute.Default}");
            textBuilder.AppendLine();

            // Option help text
            if (attribute.HelpText != null)
                textBuilder.AppendLine($"_{attribute.HelpText}_");

            // Min & Max
            bool min = attribute.Min >= 0;
            bool max = attribute.Max >= 0;
            if (min && max) {
                textBuilder.AppendLine($"Range: {attribute.Min} - {attribute.Max}");
            } else if (min) {
                textBuilder.AppendLine($"Min: {attribute.Min}");
            } else if (max) {
                textBuilder.AppendLine($"Max: {attribute.Max}");
            }

            if (!attribute.Required)
                nameBuilder.Append("]");
        }
    }
}
