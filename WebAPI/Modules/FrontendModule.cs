using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Nancy;
using Nancy.Security;

namespace WebAPI.Modules
{
    public class FrontendModule : NancyModule
    {
        public FrontendModule() : base("/")
        {
            this.RequiresAuthentication();
            Get("/", GetFrontendAsync);
        }

        private async Task<Response> GetFrontendAsync(dynamic args, CancellationToken cancellationToken)
        {
            this.RequiresAuthentication();
            return Response.AsFile("./frontend/index.html");
        }
    }
}
