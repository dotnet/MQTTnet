using System;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Client;
using MQTTnet.Core.Serializer;

namespace MQTTnet.NETFramework
{
    public class MqttClientFactory
    {
        public MqttClient CreateMqttClient(MqttClientOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            return new MqttClient(options, new MqttChannelAdapter(new MqttTcpChannel(), new DefaultMqttV311PacketSerializer()));
        }
    }
}
