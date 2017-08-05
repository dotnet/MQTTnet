using System;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Client;
using MQTTnet.Core.Serializer;
using MQTTnet.Implementations;

namespace MQTTnet
{
    public class MqttClientFactory
    {
        public IMqttClient CreateMqttClient(MqttClientOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            return new MqttClient(options, new MqttChannelCommunicationAdapter(new MqttTcpChannel(), new DefaultMqttV311PacketSerializer()));
        }
    }
}