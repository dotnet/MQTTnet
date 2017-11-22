using System;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Core.Packets;

namespace MQTTnet.Core.Adapter
{
    public static class MqttChannelAdapterExtensions
    {
        public static Task SendPacketsAsync(this IMqttChannelAdapter adapter, TimeSpan timeout, CancellationToken cancellationToken, params MqttBasePacket[] packets)
        {
            if (adapter == null) throw new ArgumentNullException(nameof(adapter));

            return adapter.SendPacketsAsync(timeout, cancellationToken, packets);
        }
    }
}