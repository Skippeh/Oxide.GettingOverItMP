using System;
using CommandLineParser.Exceptions;
using Nancy.Hosting.Self;

namespace WebAPI
{
    internal class Program
    {
        public static LaunchArguments LaunchArguments { get; private set; }

        static int Main(string[] args)
        {
            var parser = new CommandLineParser.CommandLineParser();
            LaunchArguments = new LaunchArguments();

            try
            {
                parser.ExtractArgumentAttributes(LaunchArguments);
                parser.ParseCommandLine(args);
            }
            catch (CommandLineException ex)
            {
                Console.WriteLine(ex.Message);
                parser.ShowUsage();
                return 1;
            }

            var config = new HostConfiguration
            {
                UrlReservations = new UrlReservations
                {
                    CreateAutomatically = true
                },
                RewriteLocalhost = true
            };

            using (var host = new NancyHost(config, new Uri($"http://localhost:{LaunchArguments.Port}")))
            {
                host.Start();
                Console.WriteLine("Server started, press CTRL+Q to stop.");

                while (true)
                {
                    var key = Console.ReadKey(true);

                    if ((key.Modifiers & ConsoleModifiers.Control) == ConsoleModifiers.Control && key.Key == ConsoleKey.Q)
                    {
                        break;
                    }
                }
            }

            return 0;
        }
    }
}
