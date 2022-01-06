using MQTTnet.Server;
using System;

namespace MQTTnet.AspNetCore
{
    public sealed class AspNetMqttServerOptionsBuilder : MqttServerOptionsBuilder
    {
        public AspNetMqttServerOptionsBuilder(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public IServiceProvider ServiceProvider { get; }
    }
}
