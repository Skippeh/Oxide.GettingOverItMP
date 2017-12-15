using CommandLineParser.Arguments;

namespace GettingOverItMP.Server
{
    internal class LaunchArguments
    {
        [ValueArgument(typeof(string), "hostname", Description = "The server name that should be shown in the server browser.", Optional = false)]
        public string ServerName;
    }
}
