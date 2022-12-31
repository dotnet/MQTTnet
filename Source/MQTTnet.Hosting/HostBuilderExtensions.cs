using MQTTnet.Hosting;
using System;

namespace Microsoft.Extensions.Hosting
{
    public static class HostBuilderExtensions
    {

        public static IHostBuilder UseHostedMqttServerWithServices(this IHostBuilder hostBuilder, Action<HostingMqttServerOptionsBuilder> configure)
        {
            hostBuilder.ConfigureServices((context, services) =>
            {
                
            });

            return hostBuilder;
        }

    }
}
