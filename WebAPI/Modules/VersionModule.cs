using System.Threading;
using System.Threading.Tasks;
using Nancy;

namespace WebAPI.Modules
{
    public sealed class VersionModule : NancyModule
    {
        public VersionModule() : base("/version")
        {
            Get("/", GetVersion);
        }

        private async Task<dynamic> GetVersion(dynamic o, CancellationToken cancellationToken)
        {
            return Response.AsJson(new {test = 1});
        }
    }
}
