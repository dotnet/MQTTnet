using System;
using System.IO;
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

            var writeBuffer = PacketSerializer.Serialize(packet);
            _sendTask = SendAsync( writeBuffer );
            return ExecuteWithTimeoutAsync(_sendTask, timeout);
        }

        private Task _sendTask = Task.FromResult(0); // this task is used to prevent overlapping write

        private async Task SendAsync(byte[] buffer)
        {
            await _sendTask.ConfigureAwait(false);
            await _channel.Stream.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait( false );
        }

        public async Task<MqttBasePacket> ReceivePacketAsync(TimeSpan timeout)
        {
            Tuple<MqttPacketHeader, MemoryStream> tuple;
            if (timeout > TimeSpan.Zero)
            {
                tuple = await ExecuteWithTimeoutAsync(ReceiveAsync(), timeout).ConfigureAwait(false);
            }
            else
            {
                tuple = await ReceiveAsync().ConfigureAwait(false);
            }

            var packet = PacketSerializer.Deserialize(tuple.Item1, tuple.Item2);

            if (packet == null)
            {
                throw new MqttProtocolViolationException("Received malformed packet.");
            }

            MqttTrace.Information(nameof(MqttChannelCommunicationAdapter), "RX <<< {0}", packet);
            return packet;
        }

        private async Task<Tuple<MqttPacketHeader, MemoryStream>> ReceiveAsync()
        {
            var header = MqttPacketReader.ReadHeaderFromSource(_channel.Stream);

            MemoryStream body = null;
            if (header.BodyLength > 0)
            {
                var totalRead = 0;
                var readBuffer = new byte[header.BodyLength];
                do
                {
                    var read = await _channel.Stream.ReadAsync(readBuffer, totalRead, header.BodyLength - totalRead)
                        .ConfigureAwait( false );
                    totalRead += read;
                } while (totalRead < header.BodyLength);
                body = new MemoryStream(readBuffer, 0, header.BodyLength);
            }
            else
            {
                body = new MemoryStream();
            }

            return Tuple.Create(header, body);
        }

        private static async Task<TResult> ExecuteWithTimeoutAsync<TResult>(Task<TResult> task, TimeSpan timeout)
        {
            var timeoutTask = Task.Delay(timeout);
            if (await Task.WhenAny(timeoutTask, task).ConfigureAwait(false) == timeoutTask)
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
            if (await Task.WhenAny(timeoutTask, task).ConfigureAwait(false) == timeoutTask)
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