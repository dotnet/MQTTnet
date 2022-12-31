using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTTnet.Hosting
{
    public class HostingMqttServerOptionsBuilder : MqttServerOptionsBuilder
    {
        private readonly IServiceProvider _serviceProvider;

        public HostingMqttServerOptionsBuilder(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IServiceProvider ServiceProvider => _serviceProvider;

    }
}
