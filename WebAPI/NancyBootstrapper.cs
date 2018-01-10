using Nancy;
using Nancy.Configuration;
using Nancy.TinyIoc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

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
    }
}
