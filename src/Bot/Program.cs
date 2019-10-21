using System;
using System.Linq;
using System.Threading.Tasks;

namespace Bot {
    public class Program {
        public static void Main(string[] args) {
            Program program = new Program();
            while (program._running) {
                try {
                    program.MainAsync().Wait();
                } catch (Exception ex) {
                    Console.WriteLine($"FATAL: Bot crashed. Restarting it...\n{ex}");
                }
            }
        }

        public async Task MainAsync() {
            Bot bot = new Bot();
            if (await bot.StartAsync().ConfigureAwait(false))
                await this.ConsoleHandler().ConfigureAwait(false);
            else
                Console.Read();
        }

        public Task ConsoleHandler() {
            while (this._running) {
                string input = Console.ReadLine();
                if (input == null)
                    continue;

                string[] components = input.Split(' ');
                string cmd = components.First();
                string[] args = components.Skip(1).ToArray();

                // Add a way to send messages into channels and stuff
                switch (cmd) {
                    case "exit":
                        this._running = false;
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

        private bool _running = true;
    }
}
