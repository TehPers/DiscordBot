﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BotV2.Services;
using BotV2.Services.FireEmblem;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace BotV2.CommandModules.FireEmblem
{
    public sealed class FehModule : BaseCommandModule
    {
        private static readonly Regex HexColorRegex = new Regex("#?(?:(?<r>[0-9a-fA-F]{2})(?<g>[0-9a-fA-F]{2})(?<b>[0-9a-fA-F]{2})|(?<r>[0-9a-fA-F])(?<g>[0-9a-fA-F])(?<b>[0-9a-fA-F]))");
        private readonly IFehDataProvider _dataProvider;
        private readonly EmbedService _embedService;

        public FehModule(IFehDataProvider dataProvider, EmbedService embedService)
        {
            this._dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
            this._embedService = embedService ?? throw new ArgumentNullException(nameof(embedService));
        }

        [Command("skills")]
        [Description("Searches FEH skills.")]
        [RequirePermissions(Permissions.SendMessages)]
        public async Task Skills(
            CommandContext context,
            [RemainingText] [Description("Search text.")] string query
        )
        {
            var results = await this._dataProvider.GetSkill(query);
            var embed = this.FormatResponse(results).Build();
            await context.RespondAsync(embed: embed);
        }

        [Command("stats")]
        [Description("Searches FEH stats.")]
        public async Task Stats(
            CommandContext context,
            [RemainingText] [Description("Search text.")] string query
        )
        {
            var results = await this._dataProvider.GetCharacter(query);
            var embed = this.FormatResponse(results).Build();
            await context.RespondAsync(embed: embed);
        }

        [Command("weapons")]
        [Description("Searches FEH weapons.")]
        public async Task Weapons(
            CommandContext context,
            [RemainingText] [Description("Search text.")] string query
        )
        {
            var results = await this._dataProvider.GetWeapon(query);
            var embed = this.FormatResponse(results).Build();
            await context.RespondAsync(embed: embed);
        }

        private DiscordEmbedBuilder FormatResponse(IEnumerable<KeyValuePair<string, string>> properties)
        {
            var builder = this._embedService.CreateStandardEmbed();
            builder.WithColor(new DiscordColor(1f, 0f, 0f));
            var description = new StringBuilder();
            if (properties is { })
            {
                foreach (var (key, value) in properties)
                {
                    if (value == null)
                    {
                        continue;
                    }

                    if (string.Equals(key, "Image", StringComparison.OrdinalIgnoreCase))
                    {
                        builder.WithThumbnailUrl(value);
                    }
                    else if (string.Equals(key, "Color", StringComparison.OrdinalIgnoreCase))
                    {
                        if (FehModule.HexColorRegex.Match(value) is { Success: true } match)
                        {
                            var r = byte.Parse(match.Groups["r"].Value.PadRight(2, '0'), NumberStyles.HexNumber);
                            var g = byte.Parse(match.Groups["g"].Value.PadRight(2, '0'), NumberStyles.HexNumber);
                            var b = byte.Parse(match.Groups["b"].Value.PadRight(2, '0'), NumberStyles.HexNumber);

                            builder.WithColor(new DiscordColor(r, g, b));
                        }
                    }
                    else if (string.Equals(key, "Name", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            builder.WithTitle(value);
                        }
                    }
                    else
                    {
                        description.AppendLine($"{key}: {value}");
                    }
                }
            }
            else
            {
                description.AppendLine("**No results.**");
            }

            return builder.WithDescription(description.ToString());
        }
    }
}