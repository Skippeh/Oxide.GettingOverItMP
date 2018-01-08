using CommandLineParser.Arguments;

namespace WebAPI
{
    public class LaunchArguments
    {
        [ValueArgument(typeof(short), 'p', "port", Description = "The port to listen on.", Optional = false)]
        public short Port { get; set; }
    }
}
