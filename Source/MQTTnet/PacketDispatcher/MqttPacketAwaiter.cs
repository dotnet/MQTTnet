using System;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Packets;

namespace MQTTnet.PacketDispatcher
{
    public sealed class MqttPacketAwaiter<TPacket> : IMqttPacketAwaiter where TPacket : MqttBasePacket
    {
        private readonly TaskCompletionSource<MqttBasePacket> _taskCompletionSource = new TaskCompletionSource<MqttBasePacket>();
        private readonly ushort? _packetIdentifier;
        private readonly MqttPacketDispatcher _owningPacketDispatcher;
        
        public MqttPacketAwaiter(ushort? packetIdentifier, MqttPacketDispatcher owningPacketDispatcher)
        {
            _packetIdentifier = packetIdentifier;
            _owningPacketDispatcher = owningPacketDispatcher ?? throw new ArgumentNullException(nameof(owningPacketDispatcher));
        }

        public async Task<TPacket> WaitOneAsync(TimeSpan timeout)
        {
            using (var timeoutToken = new CancellationTokenSource(timeout))
            {
                timeoutToken.Token.Register(() => _taskCompletionSource.TrySetCanceled());

                var packet = await _taskCompletionSource.Task.ConfigureAwait(false);
                return (TPacket)packet;
            }
        }

        public void Complete(MqttBasePacket packet)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));
            
            // To prevent deadlocks it is required to call the _TrySetResult_ method
            // from a new thread because the awaiting code will not(!) be executed in
            // a new thread automatically (due to await). Furthermore _this_ thread will
            // do it. But _this_ thread is also reading incoming packets -> deadlock.
            Task.Run(() => _taskCompletionSource.TrySetResult(packet));
        }

        public void Fail(Exception exception)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));

            Task.Run(() => _taskCompletionSource.TrySetException(exception));
        }

        public void Cancel()
        {
            Task.Run(() => _taskCompletionSource.TrySetCanceled());
        }

        public void Dispose()
        {
            _owningPacketDispatcher.RemovePacketAwaiter<TPacket>(_packetIdentifier);
        }
    }
}