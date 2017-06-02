using System;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Server;

namespace MQTTnet
{
    public class MqttServerFactory
    {
        public MqttServer CreateMqttServer(MqttServerOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            
            return new MqttServer(options, options.UseSSL ? (IMqttServerAdapter) new MqttSslServerAdapter() : new MqttServerAdapter());
        }
    }
}
