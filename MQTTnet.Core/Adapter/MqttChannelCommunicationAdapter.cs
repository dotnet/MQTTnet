using System;
using System.Threading.Tasks;
using MQTTnet.Core.Channel;
using MQTTnet.Core.Client;
using MQTTnet.Core.Diagnostics;
using MQTTnet.Core.Exceptions;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Serializer;

namespace MQTTnet.Core.Adapter
{
    public class MqttChannelCommunicationAdapter : IMqttCommunicationAdapter
    {
        private readonly IMqttCommunicationChannel _channel;

        public MqttChannelCommunicationAdapter(IMqttCommunicationChannel channel, IMqttPacketSerializer serializer)
        {
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
            PacketSerializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        public IMqttPacketSerializer PacketSerializer { get; }

        public async Task ConnectAsync(MqttClientOptions options, TimeSpan timeout)
        {
            var task = _channel.ConnectAsync(options);
            if (await Task.WhenAny(Task.Delay(timeout), task) != task)
            {
                throw new MqttCommunicationTimedOutException();
            }
        }

        public async Task DisconnectAsync()
        {
            await _channel.DisconnectAsync();
        }

        public async Task SendPacketAsync(MqttBasePacket packet, TimeSpan timeout)
        {
            MqttTrace.Information(nameof(MqttChannelCommunicationAdapter), $"TX >>> {packet} [Timeout={timeout}]");

            bool hasTimeout;
            try
            {
                var task = PacketSerializer.SerializeAsync(packet, _channel);
                hasTimeout = await Task.WhenAny(Task.Delay(timeout), task) != task;
            }
            catch (Exception exception)
            {
                throw new MqttCommunicationException(exception);
            }

            if (hasTimeout)
            {
                throw new MqttCommunicationTimedOutException();
            }
        }

        public async Task<MqttBasePacket> ReceivePacketAsync(TimeSpan timeout)
        {
            MqttBasePacket packet;
            if (timeout > TimeSpan.Zero)
            {
                var workerTask = PacketSerializer.DeserializeAsync(_channel);
                var timeoutTask = Task.Delay(timeout);
                var hasTimeout = Task.WhenAny(timeoutTask, workerTask) == timeoutTask;

                if (hasTimeout)
                {
                    throw new MqttCommunicationTimedOutException();
                }

                packet = workerTask.Result;
            }
            else
            {
                packet = await PacketSerializer.DeserializeAsync(_channel);
            }

            if (packet == null)
            {
                throw new MqttProtocolViolationException("Received malformed packet.");
            }

            MqttTrace.Information(nameof(MqttChannelCommunicationAdapter), $"RX <<< {packet}");
            return packet;
        }
    }
}