using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotV2.Models.WarframeInfo;
using BotV2.Services.Data;
using DSharpPlus.Entities;
using Microsoft.Extensions.Options;
using Warframe.World.Models;

namespace BotV2.Services
{
    public class WarframeInfoService
    {
        private const string BaseKey = "wfinfo";
        private const string SubscriptionsKey = WarframeInfoService.BaseKey + ":subscriptions";

        private readonly IDataService _dataService;
        private readonly IOptionsMonitor<WarframeInfoConfig> _config;

        public WarframeInfoService(IDataService dataService, IOptionsMonitor<WarframeInfoConfig> config)
        {
            this._dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            this._config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async Task SetSubscribed(ulong channelId, InfoType infoType, bool subscribed)
        {
            var globalStore = this._dataService.GetGlobalStore();
            var subscribers = globalStore.GetSetResource<ulong>(this.GetSubscriptionKey(infoType));

            if (subscribed)
            {
                await subscribers.AddAsync(channelId).ConfigureAwait(false);
            }
            else
            {
                await subscribers.RemoveAsync(channelId).ConfigureAwait(false);
            }
        }

        public async Task<bool> GetSubscribed(ulong channelId, InfoType infoType)
        {
            var globalStore = this._dataService.GetGlobalStore();
            var subscribers = globalStore.GetSetResource<ulong>(this.GetSubscriptionKey(infoType));

            return await subscribers.ContainsAsync(channelId).ConfigureAwait(false);
        }

        public async Task<bool> ToggleSubscription(ulong channelId, InfoType infoType)
        {
            var globalStore = this._dataService.GetGlobalStore();
            var subscribers = globalStore.GetSetResource<ulong>(this.GetSubscriptionKey(infoType));

            if (await subscribers.RemoveAsync(channelId).ConfigureAwait(false))
            {
                return false;
            }

            return await subscribers.AddAsync(channelId).ConfigureAwait(false);
        }

        public IAsyncEnumerable<ulong> GetSubscribers(InfoType infoType)
        {
            var globalStore = this._dataService.GetGlobalStore();
            var subscribers = globalStore.GetSetResource<ulong>(this.GetSubscriptionKey(infoType));

            return subscribers;
        }

        [SuppressMessage("ReSharper", "HeuristicUnreachableCode", Justification = "LINQ does not currently support nullable reference types, so the compiler doesn't know that FirstOrDefault can return null.")]
        public IEnumerable<DiscordRole> GetRolesForRewards(DiscordGuild guild, IEnumerable<StackedItem> items)
        {
            _ = items ?? throw new ArgumentNullException(nameof(items));
            _ = guild ?? throw new ArgumentNullException(nameof(guild));

            var enumeratedFilter = this._config.CurrentValue.ImportantRewards ?? new List<string>();
            foreach (var item in items)
            {
                if (!(enumeratedFilter.FirstOrDefault(filter => item.Type.Contains(filter, StringComparison.OrdinalIgnoreCase)) is { } matchedFilter))
                {
                    continue;
                }

                foreach (var role in guild.Roles.Values)
                {
                    if (string.Equals(role.Name, $"wfinfo: {matchedFilter}"))
                    {
                        yield return role;
                    }
                }
            }
        }

        public IEnumerable<DiscordRole> GetRolesForCycle(DiscordGuild guild, CetusCycle cycle)
        {
            _ = cycle ?? throw new ArgumentNullException(nameof(cycle));
            _ = guild ?? throw new ArgumentNullException(nameof(guild));

            var name = cycle.IsDay ? "day" : "night";
            foreach (var role in guild.Roles.Values)
            {
                if (string.Equals(role.Name, $"wfinfo: {name}"))
                {
                    yield return role;
                }
            }
        }

        public IEnumerable<StackedItem> GetImportantRewards(MissionReward reward)
        {
            _ = reward ?? throw new ArgumentNullException(nameof(reward));

            var enumeratedFilter = this._config.CurrentValue.ImportantRewards ?? new List<string>();
            var items = this.ToStackedItems(reward);
            return items.Where(item => enumeratedFilter.Any(filter => item.Type.Contains(filter, StringComparison.OrdinalIgnoreCase)));
        }

        public IEnumerable<string> GetItemStrings(IEnumerable<StackedItem> items)
        {
            _ = items ?? throw new ArgumentNullException(nameof(items));

            var icons = (this._config.CurrentValue.RewardIcons ?? Enumerable.Empty<KeyValuePair<string, string>>()).ToList();
            var currencies = new HashSet<string>(this._config.CurrentValue.Currencies ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            var sb = new StringBuilder();
            foreach (var item in items)
            {
                var isCurrency = currencies.Any(s => s.Contains(item.Type, StringComparison.OrdinalIgnoreCase));
                if (isCurrency)
                {
                    sb.Append(item.Count);
                    sb.Append("x");
                }
                else if (item.Count > 1)
                {
                    yield return $"{item.Count}";
                }

                sb.Append(" ");
                if (icons.FirstOrDefault(kv => item.Type.Contains(kv.Key, StringComparison.OrdinalIgnoreCase)) is { Value: string icon })
                {
                    sb.Append(icon);
                    if (!isCurrency)
                    {
                        sb.Append(item.Type);
                    }
                }
                else
                {
                    sb.Append(item.Type);
                }

                yield return sb.ToString();
                sb.Clear();
            }
        }

        private IEnumerable<StackedItem> ToStackedItems(MissionReward reward)
        {
            var items = reward.Items.Select(item => new StackedItem(item, 1));
            items = items.Concat(reward.Resources.Select(item => new StackedItem(item.Type, item.Count)));
            return reward.Credits > 0 ? items.Prepend(new StackedItem("credits", reward.Credits)) : items;
        }

        public IEnumerable<string> GetImportantRewards()
        {
            return this._config.CurrentValue.ImportantRewards ?? new List<string>();
        }

        public IReadOnlyDictionary<string, string> GetRewardIcons()
        {
            return this._config.CurrentValue.RewardIcons ?? new Dictionary<string, string>();
        }

        public IEnumerable<string> GetCurrencies()
        {
            return this._config.CurrentValue.Currencies ?? new List<string>();
        }

        public string GetDayIcon()
        {
            return this._config.CurrentValue.DayIcon ?? string.Empty;
        }

        public string GetNightIcon()
        {
            return this._config.CurrentValue.NightIcon ?? string.Empty;
        }

        private string GetSubscriptionKey(InfoType infoType)
        {
            return $"{WarframeInfoService.SubscriptionsKey}:{this.GetSubKey(infoType)}";
        }

        private string GetSubKey(InfoType infoType)
        {
            return infoType switch
            {
                InfoType.Alerts => "alerts",
                InfoType.Invasions => "invasions",
                InfoType.Cetus => "cetus",
                _ => throw new ArgumentOutOfRangeException(nameof(infoType)),
            };
        }

        public enum InfoType
        {
            Alerts,
            Invasions,
            Cetus,
        }
    }
}