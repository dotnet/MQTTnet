using System;
using System.Collections.Generic;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Server;
using MQTTnet.Implementations;

namespace MQTTnet
{
    public class MqttServerFactory : IMqttServerFactory
    {
        public IMqttServer CreateMqttServer(MqttServerOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            return new MqttServer(options, new List<IMqttServerAdapter> { new MqttServerAdapter() });
        }
    }
}
