using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Discord;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;

namespace Bot.Commands {
    public class CommandFEH : Command {
        private const string SHEET = "1x8QcIebWWDwkjAv0smQsJEqGp_AyOc_QhPgBrdGTxaE";

        public ConcurrentDictionary<string, SheetData> Sheets { get; set; } = new ConcurrentDictionary<string, SheetData>();

        private readonly SheetsService _service;

        public string Category { get; }

        public CommandFEH(string name, string category) : base(name) {
            this.Category = category;
            this.AddVerb<Options>();
            this.WithDescription("Displays stats");

            string secretsFile = Path.Combine(Directory.GetCurrentDirectory(), "Secret", "sheets.json");
            if (!File.Exists(secretsFile)) {
                Bot.Instance.Log($"Secrets file is missing: {secretsFile}");
            } else {

                UserCredential credentials;
                using (FileStream stream = new FileStream(secretsFile, FileMode.OpenOrCreate, FileAccess.Read)) {
                    string credPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                    credPath = Path.Combine(credPath, ".credentials/sheets.googleapis.com-tehbot.json");

                    credentials = GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.Load(stream).Secrets,
                        new[] { SheetsService.Scope.SpreadsheetsReadonly },
                        "user",
                        CancellationToken.None,
                        new FileDataStore(credPath, true)).Result;
                    Bot.Instance.Log("Credential file saved to: " + credPath, LogSeverity.Verbose, "GOOGLE");
                }

                this._service = new SheetsService(new BaseClientService.Initializer {
                    HttpClientInitializer = credentials,
                    ApplicationName = "Teh's Discord Bot"
                });
            }
        }

        public override Task Load() {
            return this.ReloadStats();
        }

        public async Task<bool> ReloadStats() {
            this.Sheets.Clear();

            Spreadsheet spreadsheet = await this._service.Spreadsheets.Get(CommandFEH.SHEET).ExecuteAsync();
            IList<Sheet> sheets = spreadsheet.Sheets;

            foreach (Sheet sheet in sheets) {
                string sheetName = sheet.Properties.Title;
                SheetData sheetData = this.Sheets.GetOrAdd(sheetName, k => new SheetData());

                // Get stat names
                ValueRange response = this._service.Spreadsheets.Values.Get(CommandFEH.SHEET, $"{sheetName}!A2:A").Execute();
                foreach (object stat in response.Values.SelectMany(row => row))
                    sheetData.StatNames.Enqueue(stat.ToString());

                // Get stats
                response = this._service.Spreadsheets.Values.Get(CommandFEH.SHEET, $"{sheetName}!A1:{sheetData.StatNames.Count + 1}").Execute();
                IList<object> firstRow = response.Values.First();
                foreach (IList<object> row in response.Values.Skip(1)) {
                    string stat = row.First().ToString();

                    for (int i = 1; i < row.Count; i++) {
                        string value = row.Count > i ? row[i].ToString() : null;
                        string key = firstRow.Count > i ? firstRow[i].ToString() : null;

                        if (key != null && value != null)
                            sheetData.Stats.GetOrAdd(key, k => new ConcurrentDictionary<string, string>()).AddOrUpdate(stat, value, (k, v) => value);
                    }
                }
            }

            return true;
        }

        public async Task ShowStats(IMessage msg, string sheet, string query) {
            SheetData sheetData = this.Sheets.FirstOrDefault(kv => kv.Key.Equals(sheet, StringComparison.OrdinalIgnoreCase)).Value;
            if (sheetData == null) {
                await msg.Channel.SendMessageAsync($"{msg.Author.Mention} Category '{sheet}' doesn't exist. Possible categories: {string.Join(", ", this.Sheets.Select(kv => $"{kv.Key}"))}");
                return;
            }

            // Find the key with the given name
            IEnumerable<string> matches = sheetData.Stats.Keys.Where(k => string.Equals(k, query, StringComparison.OrdinalIgnoreCase)).ToList();

            // If no matches, try to find the keys containing the string
            if (!matches.Any())
                matches = sheetData.Stats.Keys.Where(k => k.IndexOf(query, StringComparison.OrdinalIgnoreCase) != -1);

            if (matches.Any()) {
                string chosen = matches.First();
                List<string> statNamesList = sheetData.StatNames.ToList();

                EmbedBuilder embed = new EmbedBuilder();

                // Title
                if (sheetData.Stats[chosen].TryGetValue("Name", out string name) && !string.IsNullOrEmpty(name))
                    embed.WithTitle(name);

                // Color
                if (sheetData.Stats[chosen].TryGetValue("Hex", out string hex) && uint.TryParse(hex.TrimStart('#'), System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint color))
                    embed.WithColor(color);
                else
                    embed.WithColor(1F, 0F, 0F);

                // Stats
                embed.WithDescription(string.Join("\n", from kv in sheetData.Stats[chosen]
                                                        where !string.IsNullOrEmpty(kv.Value)
                                                              && kv.Key != "Name"
                                                              && kv.Key != "Image"
                                                              && kv.Key != "Hex"
                                                        orderby statNamesList.IndexOf(kv.Key)
                                                        select $"{kv.Key}: {kv.Value}"));

                // Image
                if (sheetData.Stats[chosen].TryGetValue("Image", out string imageLink) && !string.IsNullOrEmpty(imageLink))
                    embed.WithImageUrl(imageLink);

                // Send
                await msg.Channel.SendMessageAsync(msg.Author.Mention, false, embed.Build());

            } else {
                await msg.Channel.SendMessageAsync($"{msg.Author.Mention} No stats found for '{query}'");
            }
        }

        public class Options : Verb {
            [Value(0, HelpText = "Search text", Required = true, MetaName = "query")]
            public IEnumerable<string> Query { get; set; }

            public override Task Execute(Command cmd, IMessage message, string[] args) {
                if (!(cmd is CommandFEH cmdFEH))
                    return Task.CompletedTask;

                string query = string.Join(" ", this.Query);

                if (string.IsNullOrWhiteSpace(query))
                    return cmd.ShowHelp(message, args, null);

                query = query.Trim().ToLower();

                string sheet = cmdFEH.Category;
                if (sheet != null)
                    return cmdFEH.ShowStats(message, sheet, query);

                return message.Reply($"Sheet '{cmdFEH.Category}' not found.");
            }
        }

        public class SheetData {
            public ConcurrentDictionary<string, ConcurrentDictionary<string, string>> Stats { get; set; } = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>();
            public ConcurrentQueue<string> StatNames { get; set; } = new ConcurrentQueue<string>();
        }
    }
}
