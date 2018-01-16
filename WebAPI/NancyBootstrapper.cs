using Nancy;
using Nancy.Authentication.Basic;
using Nancy.Bootstrapper;
using Nancy.Configuration;
using Nancy.Conventions;
using Nancy.TinyIoc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using WebAPI.Authentication;

namespace WebAPI
{
    public class NancyBootstrapper : DefaultNancyBootstrapper
    {
        public sealed class CustomJsonSerializer : JsonSerializer
        {
            public CustomJsonSerializer()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver();
            }
        }

        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);
            pipelines.EnableBasicAuthentication(new BasicAuthenticationConfiguration(new BasicUserValidator(), "GOIMP WebAPI"));
        }

        public override void Configure(INancyEnvironment environment)
        {
            base.Configure(environment);

#if DEBUG
            environment.Tracing(true, true);
#endif
        }

        protected override void ConfigureApplicationContainer(TinyIoCContainer container)
        {
            base.ConfigureApplicationContainer(container);

            container.Register<JsonSerializer, CustomJsonSerializer>();
        }

        protected override void ConfigureConventions(NancyConventions nancyConventions)
        {
            base.ConfigureConventions(nancyConventions);
            nancyConventions.StaticContentsConventions.AddDirectory("/frontend", "frontend");
        }
    }
}
