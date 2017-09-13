using System;
using System.Threading.Tasks;
using MQTTnet.Core.Packets;

namespace MQTTnet.Core.Adapter
{
    public static class MqttCommunicationAdapterExtensions
    {
        public static Task SendPacketsAsync(this IMqttCommunicationAdapter adapter, TimeSpan timeout, params MqttBasePacket[] packets)
        {
            return adapter.SendPacketsAsync(timeout, packets);
        }
    }
}