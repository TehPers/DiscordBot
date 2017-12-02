﻿using System;
using System.Linq;
using System.Threading.Tasks;

namespace Bot
{
    public class Program
    {
        public static void Main(string[] args) => new Program().MainAsync().Wait();

        public async Task MainAsync()
        {
            Bot bot = new Bot();
            if (await bot.StartAsync())
                await this.ConsoleHandler();
            else
                Console.Read();
        }

        public Task ConsoleHandler()
        {
            while (this.Running)
            {
                string input = Console.ReadLine();
                if (input == null)
                    continue;

                string[] components = input.Split(' ');
                string cmd = components.First();
                string[] args = components.Skip(1).ToArray();

                // Add a way to send messages into channels and stuff
                switch (cmd)
                {
                    case "exit":
                        this.Running = false;
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

        public bool Running = true;
    }
}
