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
    public class MqttChannelAdapter : IMqttAdapter
    {
        private readonly IMqttPacketSerializer _serializer;
        private readonly IMqttTransportChannel _channel;

        public MqttChannelAdapter(IMqttTransportChannel channel, IMqttPacketSerializer serializer)
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            if (serializer == null) throw new ArgumentNullException(nameof(serializer));

            _channel = channel;
            _serializer = serializer;
        }

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
            MqttTrace.Information(nameof(MqttChannelAdapter), $"Sending with timeout {timeout} >>> {packet}");

            bool hasTimeout;
            try
            {
                var task = _serializer.SerializeAsync(packet, _channel);
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

        public async Task<MqttBasePacket> ReceivePacket()
        {
            var mqttPacket = await _serializer.DeserializeAsync(_channel);
            if (mqttPacket == null)
            {
                throw new MqttProtocolViolationException("Received malformed packet.");
            }

            return mqttPacket;
        }
    }
}