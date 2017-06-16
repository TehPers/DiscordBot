using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;
using TehPers.Discord.TehBot.Commands;

namespace TehPers.Discord.TehBot {
    public class Program {
        public static void Main(string[] args) => Task.WaitAll(new Program().MainAsync());

        public async Task MainAsync() {
            Bot bot = new Bot();
            await bot.StartAsync();

            // Enter the console loop
            await ConsoleHandler();
        }
        
        public Task ConsoleHandler() {
            while (Program.Running) {
                string input = Console.ReadLine();
                if (input == null)
                    continue;

                string[] components = input.Split(' ');
                string cmd = components.First();
                string[] args = components.Skip(1).ToArray();

                // Add a way to send messages into channels and stuff
                switch (cmd) {
                    case "exit":
                        Program.Running = false;
                        break;
                    case "help":
                        Console.WriteLine("Type 'exit' to exit");
                        break;
                    default:
                        break;
                }
            }

            return Task.CompletedTask;
        }

        public static bool Running = true;
    }
}
