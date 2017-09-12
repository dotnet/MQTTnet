using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MQTTnet.Core.Client;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Serializer;

namespace MQTTnet.Core.Adapter
{
    public interface IMqttCommunicationAdapter
    {
        Task ConnectAsync(MqttClientOptions options, TimeSpan timeout);

        Task DisconnectAsync();

        Task SendPacketsAsync( TimeSpan timeout, IEnumerable<MqttBasePacket> packets );

        Task<MqttBasePacket> ReceivePacketAsync(TimeSpan timeout);

        IMqttPacketSerializer PacketSerializer { get; }
    }

    public static class IMqttCommunicationAdapterExtensions
    {
        public static Task SendPacketsAsync( this IMqttCommunicationAdapter adapter, TimeSpan timeout, params MqttBasePacket[] packets )
        {
            return adapter.SendPacketsAsync( timeout, packets );
        }
    }
}
