using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.VisualBasic.FileIO;
using static TehPers.Discord.TehBot.Commands.CommandDocs;

namespace TehPers.Discord.TehBot.Commands {
    public class StatsCommand : Command {
        public override CommandDocs Documentation { get; }
        public ConcurrentDictionary<string, ConcurrentDictionary<string, string>> Stats { get; set; } = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>();
        public ConcurrentQueue<string> StatNames { get; set; } = new ConcurrentQueue<string>();

        public StatsCommand(string name) : base(name) {
            Documentation = new CommandDocs() {
                Description = "Displays the stats for whatever was matched",
                Arguments = new List<Argument>() {
                    new Argument("-r", "Reloads the stats database before grabbing the stats", true),
                    new Argument("query", "What you're trying to find stats for, ie. a character's name")
                }
            };

            ReloadStats();
        }

        private static readonly object StatsFileLock = new object();
        public bool ReloadStats() {
            lock (StatsCommand.StatsFileLock) {
                string path = Path.Combine(Directory.GetCurrentDirectory(), "stats.csv");
                if (!File.Exists(path))
                    return false;

                // Clear the loaded stats database
                Stats.Clear();
                while (StatNames.Any() && StatNames.TryDequeue(out string _)) { }

                // Load new stats
                using (TextFieldParser parser = new TextFieldParser(path)) {
                    parser.SetDelimiters(",");

                    bool header = true;
                    List<string> objects = new List<string>();
                    while (!parser.EndOfData) {
                        string[] fields = parser.ReadFields();
                        if (fields == null || !fields.Any())
                            continue;

                        if (header) {
                            // Iterate through the top row and add the names of all the objects, skipping the first column
                            foreach (string field in fields.Skip(1)) {
                                Stats[field] = new ConcurrentDictionary<string, string>();
                                objects.Add(field);
                            }
                            header = false;
                        } else {
                            // Get the name of the stat
                            string stat = fields.First();
                            StatNames.Enqueue(stat);

                            // Iterate through each object and give it the associated stat
                            for (int i = 1; i < Math.Min(fields.Length, objects.Count + 1); i++) {
                                string value = fields[i];
                                Stats[objects[i - 1]][stat] = value;
                            }
                        }
                    }

                    AddEasterEggs();
                }

                return true;
            }
        }

        private void AddEasterEggs() {
            if (Stats.TryAdd("coldsteel", new ConcurrentDictionary<string, string>())) {
                Stats["coldsteel"]["**Name**"] = "**Coldsteel the Hedgehog**";
                Stats["coldsteel"]["**Speed**"] = "Faster than sound";
                Stats["coldsteel"]["**Strength**"] = "Stronger than Sonic";
                Stats["coldsteel"]["**Usefulness**"] = "None";
                Stats["coldsteel"]["**Catchphrase**"] = "*Nothing personnel, kid.*";
                Stats["coldsteel"]["Image"] = "http://i0.kym-cdn.com/photos/images/newsfeed/000/613/323/e2e.jpg";
            }

            if (Stats.TryAdd("saturtaco", new ConcurrentDictionary<string, string>())) {
                Stats["saturtaco"]["**Name**"] = "**Saturtaco**";
                Stats["saturtaco"]["**Catchphrase**"] = "*Order for four soft tacos, please.*";
                Stats["saturtaco"]["Image"] = "https://cdn.discordapp.com/attachments/291122372722032652/325140338849218580/image.jpg";
            }
        }

        public override bool Validate(SocketMessage msg, string[] args) {
            return args.Any();
        }

        public override async Task Execute(SocketMessage msg, string[] args) {
            bool reload = args.Length > 1 && args.First() == "-r";
            string query = string.Join(" ", args.Skip(reload ? 1 : 0)).ToLower();

            if (reload && !ReloadStats()) {
                await msg.Channel.SendMessageAsync($"{msg.Author.Mention} Failed reload stats database");
                return;
            }

            await ShowStats(msg, query);
        }

        public async Task ShowStats(SocketMessage msg, string query) {
            IEnumerable<string> matches = Stats.Keys.Where(k => string.Equals(k, query, StringComparison.OrdinalIgnoreCase));

            // If no matches, try to find the keys containing the string
            if (!matches.Any())
                matches = Stats.Keys.Where(k => k.Contains(query));

            if (matches.Any()) {
                string chosen = matches.First();
                List<string> statNamesList = StatNames.ToList();

                if (Stats[chosen].TryGetValue("Image", out string imageLink) && !string.IsNullOrEmpty(imageLink)) {
                    try {
                        HttpWebRequest request = WebRequest.CreateHttp(imageLink);
                        HttpWebResponse response = (HttpWebResponse) request.GetResponse();
                        string filename = Path.GetFileName(request.RequestUri.AbsolutePath);

                        Task.WaitAny(
                            Task.Delay(2000),
                            msg.Channel.SendFileAsync(response.GetResponseStream(), filename, $"{msg.Author.Mention} Stats for '{chosen}'\n" + string.Join("\n",
                                                                                                  from kv in Stats[chosen]
                                                                                                  where !string.IsNullOrEmpty(kv.Value)
                                                                                                        && kv.Key != "Image"
                                                                                                  orderby statNamesList.IndexOf(kv.Key)
                                                                                                  select $"{kv.Key}: {kv.Value}"))
                        );
                    } catch (Exception) {
                        await msg.Channel.SendMessageAsync($"Failed to find image.\n{msg.Author.Mention} Stats for '{chosen}'\n" + string.Join("\n",
                                                               from kv in Stats[chosen]
                                                               where !string.IsNullOrEmpty(kv.Value)
                                                               orderby statNamesList.IndexOf(kv.Key)
                                                               select $"{kv.Key}: {kv.Value}"));
                    }
                } else {
                    await msg.Channel.SendMessageAsync($"{msg.Author.Mention} Stats for '{chosen}'\n" + string.Join("\n",
                                                           from kv in Stats[chosen]
                                                           where !string.IsNullOrEmpty(kv.Value)
                                                           orderby statNamesList.IndexOf(kv.Key)
                                                           select $"{kv.Key}: {kv.Value}"));
                }
            } else {
                await msg.Channel.SendMessageAsync($"{msg.Author.Mention} No stats found for '{query}'");
            }
        }
    }
}
