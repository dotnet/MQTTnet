using System;
using System.Threading.Tasks;
using MQTTnet.Core.Packets;

namespace MQTTnet.Core.Client
{
    public class MqttPacketAwaiter
    {
        private readonly TaskCompletionSource<MqttBasePacket> _taskCompletionSource = new TaskCompletionSource<MqttBasePacket>();
        private readonly Func<MqttBasePacket, bool> _packetSelector;

        public MqttPacketAwaiter(Func<MqttBasePacket, bool> packetSelector)
        {
            if (packetSelector == null) throw new ArgumentNullException(nameof(packetSelector));

            _packetSelector = packetSelector;
        }

        public Task<MqttBasePacket> Task => _taskCompletionSource.Task;

        public bool CheckPacket(MqttBasePacket packet)
        {
            if (!_packetSelector(packet))
            {
                return false;
            }

            _taskCompletionSource.SetResult(packet);
            return true;
        }
    }
}