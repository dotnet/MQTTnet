using MQTTnet.Server;
using System;

namespace MQTTnet.AspNetCore
{
    public class AspNetMqttServerOptionsBuilder : MqttServerOptionsBuilder
    {
        public AspNetMqttServerOptionsBuilder(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        public IServiceProvider ServiceProvider { get; }
    }
}
