using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Diagnostics;
using MQTTnet.Exceptions;
using MQTTnet.Packets;

namespace MQTTnet.Client
{
    public class MqttPacketDispatcher
    {
        private readonly ConcurrentDictionary<Tuple<ushort, Type>, TaskCompletionSource<MqttBasePacket>> _awaiters = new ConcurrentDictionary<Tuple<ushort, Type>, TaskCompletionSource<MqttBasePacket>>();
        private readonly IMqttNetLogger _logger;

        public MqttPacketDispatcher(IMqttNetLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<MqttBasePacket> WaitForPacketAsync(Type responseType, ushort? identifier, TimeSpan timeout)
        {
            var packetAwaiter = AddPacketAwaiter(responseType, identifier);
            try
            {
                return await Internal.TaskExtensions.TimeoutAfter(ct => packetAwaiter.Task, timeout, CancellationToken.None).ConfigureAwait(false);
            }
            catch (MqttCommunicationTimedOutException)
            {
                _logger.Warning<MqttPacketDispatcher>("Timeout while waiting for packet of type '{0}'.", responseType.Name);
                throw;
            }
            finally
            {
                RemovePacketAwaiter(responseType, identifier);
            }
        }

        public void Dispatch(MqttBasePacket packet)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));

            ushort identifier = 0;
            if (packet is IMqttPacketWithIdentifier packetWithIdentifier && packetWithIdentifier.PacketIdentifier.HasValue)
            {
                identifier = packetWithIdentifier.PacketIdentifier.Value;
            }

            var type = packet.GetType();
            var key = new Tuple<ushort, Type>(identifier, type);
            
            if (_awaiters.TryRemove(key, out var tcs))
            {
                tcs.TrySetResult(packet);
                return;
            }

            throw new InvalidOperationException($"Packet of type '{type.Name}' not handled or dispatched.");
        }

        public void Reset()
        {
            _awaiters.Clear();
        }

        private TaskCompletionSource<MqttBasePacket> AddPacketAwaiter(Type responseType, ushort? identifier)
        {
            var tcs = new TaskCompletionSource<MqttBasePacket>();

            if (!identifier.HasValue)
            {
                identifier = 0;
            }

            var dictionaryKey = new Tuple<ushort, Type>(identifier ?? 0, responseType);
            if (!_awaiters.TryAdd(dictionaryKey, tcs))
            {
                throw new InvalidOperationException($"The packet dispatcher already has an awaiter for packet of type '{responseType}' with identifier {identifier}.");
            }

            return tcs;
        }

        private void RemovePacketAwaiter(Type responseType, ushort? identifier)
        {
            if (!identifier.HasValue)
            {
                identifier = 0;
            }

            var dictionaryKey = new Tuple<ushort, Type>(identifier ?? 0, responseType);
            _awaiters.TryRemove(dictionaryKey, out var _);
        }
    }
}