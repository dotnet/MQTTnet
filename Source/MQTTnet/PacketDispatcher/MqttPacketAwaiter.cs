using System;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Exceptions;
using MQTTnet.Internal;
using MQTTnet.Packets;

namespace MQTTnet.PacketDispatcher
{
    public sealed class MqttPacketAwaiter<TPacket> : Disposable, IMqttPacketAwaiter where TPacket : MqttBasePacket
    {
        private readonly TaskCompletionSource<MqttBasePacket> _taskCompletionSource;
        private readonly ushort? _packetIdentifier;
        private readonly MqttPacketDispatcher _owningPacketDispatcher;
        
        public MqttPacketAwaiter(ushort? packetIdentifier, MqttPacketDispatcher owningPacketDispatcher)
        {
            _packetIdentifier = packetIdentifier;
            _owningPacketDispatcher = owningPacketDispatcher ?? throw new ArgumentNullException(nameof(owningPacketDispatcher));
#if NET452
            _taskCompletionSource = new TaskCompletionSource<MqttBasePacket>();
#else
            _taskCompletionSource = new TaskCompletionSource<MqttBasePacket>(TaskCreationOptions.RunContinuationsAsynchronously);
#endif
        }

        public async Task<TPacket> WaitOneAsync(TimeSpan timeout)
        {
            using (var timeoutToken = new CancellationTokenSource(timeout))
            {
                timeoutToken.Token.Register(() => Fail(new MqttCommunicationTimedOutException()));

                var packet = await _taskCompletionSource.Task.ConfigureAwait(false);
                return (TPacket)packet;
            }
        }

        public void Complete(MqttBasePacket packet)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));


#if NET452
            // To prevent deadlocks it is required to call the _TrySetResult_ method
            // from a new thread because the awaiting code will not(!) be executed in
            // a new thread automatically (due to await). Furthermore _this_ thread will
            // do it. But _this_ thread is also reading incoming packets -> deadlock.
            // NET452 does not support RunContinuationsAsynchronously
            Task.Run(() => _taskCompletionSource.TrySetResult(packet));
#else
            _taskCompletionSource.TrySetResult(packet);
#endif
        }

        public void Fail(Exception exception)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));
#if NET452
            // To prevent deadlocks it is required to call the _TrySetResult_ method
            // from a new thread because the awaiting code will not(!) be executed in
            // a new thread automatically (due to await). Furthermore _this_ thread will
            // do it. But _this_ thread is also reading incoming packets -> deadlock.
            // NET452 does not support RunContinuationsAsynchronously
            Task.Run(() => _taskCompletionSource.TrySetException(exception));
#else
            _taskCompletionSource.TrySetException(exception);
#endif
        }

        public void Cancel()
        {
#if NET452
            // To prevent deadlocks it is required to call the _TrySetResult_ method
            // from a new thread because the awaiting code will not(!) be executed in
            // a new thread automatically (due to await). Furthermore _this_ thread will
            // do it. But _this_ thread is also reading incoming packets -> deadlock.
            // NET452 does not support RunContinuationsAsynchronously
            Task.Run(() => _taskCompletionSource.TrySetCanceled());
#else
            _taskCompletionSource.TrySetCanceled();
#endif
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _owningPacketDispatcher.RemovePacketAwaiter<TPacket>(_packetIdentifier);
            }
            base.Dispose(disposing);
        }
    }
}