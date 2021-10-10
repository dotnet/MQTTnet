using MQTTnet.Exceptions;
using MQTTnet.Packets;
using System;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Internal;

namespace MQTTnet.PacketDispatcher
{
    public sealed class MqttPacketAwaitable<TPacket> : IMqttPacketAwaitable where TPacket : MqttBasePacket
    {
        readonly CrossPlatformPromise<MqttBasePacket> _promise = new CrossPlatformPromise<MqttBasePacket>();
        readonly MqttPacketDispatcher _owningPacketDispatcher;

        public MqttPacketAwaitable(ushort packetIdentifier, MqttPacketDispatcher owningPacketDispatcher)
        {
            Filter = new MqttPacketAwaitableFilter
            {
                Type = typeof(TPacket),
                Identifier = packetIdentifier
            };
            
            _owningPacketDispatcher = owningPacketDispatcher ?? throw new ArgumentNullException(nameof(owningPacketDispatcher));
        }
        
        public MqttPacketAwaitableFilter Filter { get; }
        
        public async Task<TPacket> WaitOneAsync(TimeSpan timeout)
        {
            using (var timeoutToken = new CancellationTokenSource(timeout))
            {
                using (timeoutToken.Token.Register(() => Fail(new MqttCommunicationTimedOutException())))
                {
                    var packet = await _promise.Task.ConfigureAwait(false);
                    return (TPacket)packet;
                }
            }
        }

        public void Complete(MqttBasePacket packet)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));

            _promise.TrySetResult(packet);
        }

        public void Fail(Exception exception)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));
            
            _promise.TrySetException(exception);
        }

        public void Cancel()
        {
            _promise.TrySetCanceled();
        }

        public void Dispose()
        {
            _owningPacketDispatcher.RemoveAwaitable(this);
        }
    }
}