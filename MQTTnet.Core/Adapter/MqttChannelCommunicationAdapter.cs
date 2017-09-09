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

        public Task ConnectAsync(MqttClientOptions options, TimeSpan timeout)
        {
            return ExecuteWithTimeoutAsync(_channel.ConnectAsync(options), timeout);
        }

        public Task DisconnectAsync()
        {
            return _channel.DisconnectAsync();
        }

        public Task SendPacketAsync(MqttBasePacket packet, TimeSpan timeout)
        {
            MqttTrace.Information(nameof(MqttChannelCommunicationAdapter), "TX >>> {0} [Timeout={1}]", packet, timeout);

            return ExecuteWithTimeoutAsync(PacketSerializer.SerializeAsync(packet, _channel), timeout);
        }

        public async Task<MqttBasePacket> ReceivePacketAsync(TimeSpan timeout)
        {
            MqttBasePacket packet;
            if (timeout > TimeSpan.Zero)
            {
                packet = await ExecuteWithTimeoutAsync(PacketSerializer.DeserializeAsync(_channel), timeout);
            }
            else
            {
                packet = await PacketSerializer.DeserializeAsync(_channel);
            }

            if (packet == null)
            {
                throw new MqttProtocolViolationException("Received malformed packet.");
            }

            MqttTrace.Information(nameof(MqttChannelCommunicationAdapter), "RX <<< {0}", packet);
            return packet;
        }

        private static async Task<TResult> ExecuteWithTimeoutAsync<TResult>(Task<TResult> task, TimeSpan timeout)
        {
            var timeoutTask = Task.Delay(timeout);
            if (await Task.WhenAny(timeoutTask, task) == timeoutTask)
            {
                throw new MqttCommunicationTimedOutException();
            }

            if (task.IsFaulted)
            {
                throw new MqttCommunicationException(task.Exception);
            }

            return task.Result;
        }

        private static async Task ExecuteWithTimeoutAsync(Task task, TimeSpan timeout)
        {
            var timeoutTask = Task.Delay(timeout);
            if (await Task.WhenAny(timeoutTask, task) == timeoutTask)
            {
                throw new MqttCommunicationTimedOutException();
            }

            if (task.IsFaulted)
            {
                throw new MqttCommunicationException(task.Exception);
            }
        }
    }
}