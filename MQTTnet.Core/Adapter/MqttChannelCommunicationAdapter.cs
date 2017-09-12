using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Core.Channel;
using MQTTnet.Core.Client;
using MQTTnet.Core.Diagnostics;
using MQTTnet.Core.Exceptions;
using MQTTnet.Core.Internal;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Serializer;

namespace MQTTnet.Core.Adapter
{
    public class MqttChannelCommunicationAdapter : IMqttCommunicationAdapter
    {
        private readonly IMqttCommunicationChannel _channel;
        private readonly byte[] _readBuffer = new byte[BufferConstants.Size];

        public MqttChannelCommunicationAdapter(IMqttCommunicationChannel channel, IMqttPacketSerializer serializer)
        {
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
            PacketSerializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        public IMqttPacketSerializer PacketSerializer { get; }

        public Task ConnectAsync(MqttClientOptions options, TimeSpan timeout)
        {
            return _channel.ConnectAsync(options).TimeoutAfter(timeout);
        }

        public Task DisconnectAsync()
        {
            return _channel.DisconnectAsync();
        }

        public async Task SendPacketsAsync( TimeSpan timeout, IEnumerable<MqttBasePacket> packets )
        {
            lock ( _channel )
            {
                foreach (var packet in packets )
                {
                    MqttTrace.Information( nameof( MqttChannelCommunicationAdapter ), "TX >>> {0} [Timeout={1}]", packet, timeout );

                    var writeBuffer = PacketSerializer.Serialize(packet);
                
                    _sendTask = _sendTask.ContinueWith( p => _channel.SendStream.WriteAsync( writeBuffer, 0, writeBuffer.Length ) );
                }
            }

            await _sendTask; // configure await false geneates stackoverflow
            await _channel.SendStream.FlushAsync().TimeoutAfter( timeout ).ConfigureAwait( false );
        }

        private Task _sendTask = Task.FromResult(0); // this task is used to prevent overlapping write

        public async Task<MqttBasePacket> ReceivePacketAsync(TimeSpan timeout)
        {
            Tuple<MqttPacketHeader, MemoryStream> tuple;
            if (timeout > TimeSpan.Zero)
            {
                tuple = await ReceiveAsync(_channel.RawStream).TimeoutAfter(timeout).ConfigureAwait(false);
            }
            else
            {
                tuple = await ReceiveAsync(_channel.RawStream).ConfigureAwait(false);
            }

            var packet = PacketSerializer.Deserialize(tuple.Item1, tuple.Item2);

            if (packet == null)
            {
                throw new MqttProtocolViolationException("Received malformed packet.");
            }

            MqttTrace.Information(nameof(MqttChannelCommunicationAdapter), "RX <<< {0}", packet);
            return packet;
        }

        private async Task<Tuple<MqttPacketHeader, MemoryStream>> ReceiveAsync(Stream stream)
        {
            var header = MqttPacketReader.ReadHeaderFromSource(stream);

            MemoryStream body = null;
            if (header.BodyLength > 0)
            {
                var totalRead = 0;
                do
                {
                    var read = await stream.ReadAsync(_readBuffer, totalRead, header.BodyLength - totalRead)
                        .ConfigureAwait( false );
                    totalRead += read;
                } while (totalRead < header.BodyLength);
                body = new MemoryStream(_readBuffer, 0, header.BodyLength);
            }
            else
            {
                body = new MemoryStream();
            }

            return Tuple.Create(header, body);
        }
    }
}