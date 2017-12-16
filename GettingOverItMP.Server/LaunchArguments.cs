using CommandLineParser.Arguments;

namespace GettingOverItMP.Server
{
    internal class LaunchArguments
    {
        [ValueArgument(typeof(string), "hostname", Description = "The server name that should be shown in the server browser.", Optional = false)]
        public string ServerName;

        [SwitchArgument("private", false, Description = "If enabled then the server will not appear in the server browser.")]
        public bool Private;

        [ValueArgument(typeof(int), "maxplayers", Description = "The max amount of players allowed on the server.", Optional = false)]
        public int MaxPlayers;

        [ValueArgument(typeof(int), "port", Description = "The port to listen on.", DefaultValue = 25050)]
        public int Port;
    }
}
