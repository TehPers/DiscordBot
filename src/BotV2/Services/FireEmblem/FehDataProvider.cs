using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BotV2.Models.FireEmblem;
using DuoVia.FuzzyStrings;
using Google.Apis.Sheets.v4;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BotV2.Services.FireEmblem
{
    public sealed class FehDataProvider : IFehDataProvider, IDisposable
    {
        private readonly IOptionsMonitor<FehDataProviderConfig> _configMonitor;
        private readonly SheetsService _sheets;
        private readonly ILogger<FehDataProvider> _logger;
        private readonly List<IDisposable> _disposables;
        private readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<KeyValuePair<string, string>>>> _cache;
        private readonly SemaphoreSlim _cacheUpdate;

        public FehDataProvider(IOptionsMonitor<FehDataProviderConfig> configMonitor, SheetsService sheets, ILogger<FehDataProvider> logger)
        {
            this._configMonitor = configMonitor ?? throw new ArgumentNullException(nameof(configMonitor));
            this._sheets = sheets ?? throw new ArgumentNullException(nameof(sheets));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));

            this._cache = new ConcurrentDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<KeyValuePair<string, string>>>>(StringComparer.OrdinalIgnoreCase);
            this._cacheUpdate = new SemaphoreSlim(1, 1);
            this._disposables = new List<IDisposable>
            {
                this._configMonitor.OnChange(config => this.Reload()),
                this._cacheUpdate
            };
        }

        public Task<IEnumerable<KeyValuePair<string, string>>> GetCharacter(string query)
        {
            return this.Get(this._configMonitor.CurrentValue.CharacterSheet, query);
        }

        public Task<IEnumerable<KeyValuePair<string, string>>> GetSkill(string query)
        {
            return this.Get(this._configMonitor.CurrentValue.SkillSheet, query);
        }

        public Task<IEnumerable<KeyValuePair<string, string>>> GetWeapon(string query)
        {
            return this.Get(this._configMonitor.CurrentValue.WeaponSheet, query);
        }

        public Task<IEnumerable<KeyValuePair<string, string>>> GetSeal(string query)
        {
            return this.Get(this._configMonitor.CurrentValue.SealsSheet, query);
        }

        private async Task<IEnumerable<KeyValuePair<string, string>>> Get(string sheetName, string query)
        {
            // Find the key with the given name
            var sheetData = await this.GetRawData(sheetName);
            if (sheetData.TryGetValue(query, out var entry))
            {
                return entry;
            }

            // If no matches, guess
            var sortedEntries = from kv in sheetData
                                let priority = kv.Key.Contains(query, StringComparison.OrdinalIgnoreCase) ? 0 : 1
                                let distance = query.LevenshteinDistance(kv.Key)
                                orderby priority, distance
                                select kv.Value;

            return sortedEntries.FirstOrDefault();
        }

        private async Task<IReadOnlyDictionary<string, IReadOnlyList<KeyValuePair<string, string>>>> GetRawData(string sheetName)
        {
            if (this._cache.TryGetValue(sheetName, out var sheetData))
            {
                return sheetData;
            }

            // Prevent multiple threads from loading the sheet at once
            await this._cacheUpdate.WaitAsync();
            try
            {
                // Double-check that the dictionary hasn't been updated
                if (this._cache.TryGetValue(sheetName, out sheetData))
                {
                    return sheetData;
                }

                this._logger.LogDebug($"Reloading sheet '{sheetName}'");
                var sheet = await this.LoadSheet(sheetName);
                return this._cache.GetOrAdd(sheetName, sheet);
            }
            finally
            {
                this._cacheUpdate.Release();
            }
        }

        private async Task<IReadOnlyDictionary<string, IReadOnlyList<KeyValuePair<string, string>>>> LoadSheet(string sheetName)
        {
            // Get the spreadsheet's metadata
            var spreadsheetDataRequest = this._sheets.Spreadsheets.Get(this._configMonitor.CurrentValue.SheetId);
            spreadsheetDataRequest.PrettyPrint = false;
            var spreadsheetData = await spreadsheetDataRequest.ExecuteAsync();
            if (spreadsheetData == null)
            {
                // TODO: Create exception type
                throw new NotImplementedException("The spreadsheet could not be loaded.");
            }

            var sheet = spreadsheetData.Sheets.SingleOrDefault(s => string.Equals(s.Properties.Title, sheetName, StringComparison.OrdinalIgnoreCase));
            if (sheet == null)
            {
                // TODO: Create exception type
                throw new NotImplementedException($"The sheet '{sheetName}' was not found.");
            }

            // Request the full contents of the sheet
            var request = this._sheets.Spreadsheets.Values.Get(this._configMonitor.CurrentValue.SheetId, $"'{sheet.Properties.Title}'!A1:{sheet.Properties.GridProperties.RowCount}");
            request.PrettyPrint = false;
            request.MajorDimension = SpreadsheetsResource.ValuesResource.GetRequest.MajorDimensionEnum.COLUMNS;
            var values = await request.ExecuteAsync();
            if (values.Values.Count == 0)
            {
                // TODO: Create exception type
                throw new NotImplementedException($"The sheet '{sheetName}' is empty.");
            }

            // Make sure the sheet isn't empty
            var keys = values.Values[0].Skip(1).Select(k => k.ToString()).ToArray();
            if (keys.Length == 0)
            {
                // TODO: Create exception type
                throw new NotImplementedException($"The sheet '{sheetName}' has no keys.");
            }

            var entries = new Dictionary<string, IReadOnlyList<KeyValuePair<string, string>>>(StringComparer.OrdinalIgnoreCase);
            foreach (var col in values.Values.Skip(1))
            {
                if (!col.Any())
                {
                    continue;
                }

                var name = col[0].ToString();
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var entryProperties = new List<KeyValuePair<string, string>>();
                for (var i = 1; i < Math.Min(col.Count, keys.Length + 1); i++)
                {
                    entryProperties.Add(new KeyValuePair<string, string>(keys[i - 1], col[i].ToString()));
                }

                if (!entries.TryAdd(name, entryProperties))
                {
                    this._logger.LogWarning($"Duplicate entry for '{name}', overwriting it");
                    entries[name] = entryProperties;
                }
            }

            return entries;
        }

        private void Reload()
        {
            this._cache.Clear();
        }

        public void Dispose()
        {
            foreach (var disposable in this._disposables)
            {
                disposable?.Dispose();
            }

            this._disposables.Clear();
        }
    }
}