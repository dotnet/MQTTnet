using System;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Packets;

namespace MQTTnet.Client.PacketDispatcher
{
    public sealed class MqttPacketAwaiter<TPacket> : IMqttPacketAwaiter where TPacket : MqttBasePacket
    {
        private readonly TaskCompletionSource<MqttBasePacket> _packet = new TaskCompletionSource<MqttBasePacket>();

        public async Task<TPacket> WaitOneAsync(TimeSpan timeout)
        {
            using (var timeoutToken = new CancellationTokenSource(timeout))
            {
                timeoutToken.Token.Register(() => _packet.TrySetCanceled());

                var packet = await _packet.Task.ConfigureAwait(false);
                return (TPacket)packet;
            }
        }

        public void Complete(MqttBasePacket packet)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));

            _packet.TrySetResult(packet);
        }

        public void Fail(Exception exception)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));

            _packet.TrySetException(exception);
        }
    }
}