using System;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Channel;
using MQTTnet.Core.Client;
using MQTTnet.Core.Serializer;

namespace MQTTnet
{
    public class MqttClientFactory
    {
        public MqttClient CreateMqttClient(MqttClientOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            
            return new MqttClient(options,
                // The cast to IMqttCommunicationChannel is required... Roslyn is too stupid to
                // figure out how to cast back to the base type
                new MqttChannelCommunicationAdapter(options.UseSSL ? new MqttClientSslChannel() : (IMqttCommunicationChannel)new MqttTcpChannel(),
                    new DefaultMqttV311PacketSerializer()));
        }
    }
}
