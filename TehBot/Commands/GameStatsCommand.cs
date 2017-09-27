using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using Microsoft.VisualBasic.FileIO;
using static TehPers.Discord.TehBot.Commands.CommandDocs;

namespace TehPers.Discord.TehBot.Commands {
    public class GameStatsCommand : Command {
        private const string SHEET = "1x8QcIebWWDwkjAv0smQsJEqGp_AyOc_QhPgBrdGTxaE";

        public override CommandDocs Documentation { get; }
        public ConcurrentDictionary<string, SheetData> Sheets { get; set; } = new ConcurrentDictionary<string, SheetData>();

        private readonly SheetsService _service;

        public string Category { get; }

        public GameStatsCommand(string name, string category) : base(name) {
            this.Category = category;
            this.Documentation = new CommandDocs {
                Description = "Displays the stats for whatever was matched",
                Arguments = new List<Argument> {
                    new Argument("query", "What you're trying to find stats for, ie. a character's name")
                }
            };

            UserCredential credentials;
            using (FileStream stream = new FileStream(Path.Combine(Directory.GetCurrentDirectory(), "Secret", "sheets.json"), FileMode.OpenOrCreate, FileAccess.Read)) {
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

            this.ReloadStats().Wait();
        }

        public async Task<bool> ReloadStats() {
            this.Sheets.Clear();

            Spreadsheet spreadsheet = await this._service.Spreadsheets.Get(GameStatsCommand.SHEET).ExecuteAsync();
            IList<Sheet> sheets = spreadsheet.Sheets;

            foreach (Sheet sheet in sheets) {
                string sheetName = sheet.Properties.Title;
                SheetData sheetData = this.Sheets.GetOrAdd(sheetName, k => new SheetData());

                // Get stat names
                ValueRange response = this._service.Spreadsheets.Values.Get(GameStatsCommand.SHEET, $"{sheetName}!A2:A").Execute();
                foreach (object stat in response.Values.SelectMany(row => row))
                    sheetData.StatNames.Enqueue(stat.ToString());

                // Get stats
                response = this._service.Spreadsheets.Values.Get(GameStatsCommand.SHEET, $"{sheetName}!A1:{sheetData.StatNames.Count + 1}").Execute();
                IList<object> firstRow = response.Values.First();
                foreach (IList<object> row in response.Values.Skip(1)) {
                    string stat = row.First().ToString();

                    for (int i = 1; i < row.Count; i++) {
                        string value = row[i]?.ToString();
                        string key = firstRow[i]?.ToString();

                        if (key != null && value != null)
                            sheetData.Stats.GetOrAdd(key, k => new ConcurrentDictionary<string, string>()).AddOrUpdate(stat, value, (k, v) => value);
                    }
                }
            }

            return true;
        }

        public override bool Validate(SocketMessage msg, string[] args) {
            return args.Any();
        }

        public override async Task Execute(SocketMessage msg, string[] args) {
            string sheet = this.Category;
            string query = string.Join(" ", args).ToLower();

            if (sheet != null)
                await this.ShowStats(msg, sheet, query);
        }

        public async Task ShowStats(SocketMessage msg, string sheet, string query) {
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

                if (sheetData.Stats[chosen].TryGetValue("Name", out string name) && !string.IsNullOrEmpty(name))
                    embed.WithTitle(name);

                embed.WithColor(1F, 0F, 0F);

                embed.WithDescription(string.Join("\n", from kv in sheetData.Stats[chosen]
                                                        where !string.IsNullOrEmpty(kv.Value)
                                                              && kv.Key != "Name"
                                                              && kv.Key != "Image"
                                                        orderby statNamesList.IndexOf(kv.Key)
                                                        select $"{kv.Key}\n{kv.Value}"));

                if (sheetData.Stats[chosen].TryGetValue("Image", out string imageLink) && !string.IsNullOrEmpty(imageLink))
                    embed.WithImageUrl(imageLink);

                await msg.Channel.SendMessageAsync(msg.Author.Mention, false, embed.Build());
            } else {
                await msg.Channel.SendMessageAsync($"{msg.Author.Mention} No stats found for '{query}'");
            }
        }

        public class SheetData {
            public ConcurrentDictionary<string, ConcurrentDictionary<string, string>> Stats { get; set; } = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>();
            public ConcurrentQueue<string> StatNames { get; set; } = new ConcurrentQueue<string>();
        }
    }
}
