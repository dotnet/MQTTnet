using System;
using System.Collections.Generic;
using System.IO;
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

        private Task _sendTask = Task.FromResult(0); // this task is used to prevent overlapping write

        public MqttChannelCommunicationAdapter(IMqttCommunicationChannel channel, IMqttPacketSerializer serializer)
        {
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
            PacketSerializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        public IMqttPacketSerializer PacketSerializer { get; }

        public async Task ConnectAsync(TimeSpan timeout, MqttClientOptions options)
        {
            try
            {
                await _channel.ConnectAsync(options).TimeoutAfter(timeout);
            }
            catch (MqttCommunicationTimedOutException)
            {
                throw;
            }
            catch (MqttCommunicationException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new MqttCommunicationException(exception);
            }
        }

        public async Task DisconnectAsync(TimeSpan timeout)
        {
            try
            {
                await _channel.DisconnectAsync().TimeoutAfter(timeout).ConfigureAwait(false);
            }
            catch (MqttCommunicationTimedOutException)
            {
                throw;
            }
            catch (MqttCommunicationException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new MqttCommunicationException(exception);
            }
        }

        public async Task SendPacketsAsync(TimeSpan timeout, IEnumerable<MqttBasePacket> packets)
        {
            try
            {
                lock (_channel)
                {
                    foreach (var packet in packets)
                    {
                        MqttTrace.Information(nameof(MqttChannelCommunicationAdapter), "TX >>> {0} [Timeout={1}]", packet, timeout);

                        var writeBuffer = PacketSerializer.Serialize(packet);
                        _sendTask = _sendTask.ContinueWith(p => _channel.SendStream.WriteAsync(writeBuffer, 0, writeBuffer.Length));
                    }
                }

                await _sendTask; // configure await false geneates stackoverflow

                if (timeout > TimeSpan.Zero)
                {
                    await _channel.SendStream.FlushAsync().TimeoutAfter(timeout).ConfigureAwait(false);
                }
                else
                {
                    await _channel.SendStream.FlushAsync().ConfigureAwait(false);
                }             
            }
            catch (MqttCommunicationTimedOutException)
            {
                throw;
            }
            catch (MqttCommunicationException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new MqttCommunicationException(exception);
            }
        }

        public async Task<MqttBasePacket> ReceivePacketAsync(TimeSpan timeout)
        {
            try
            {
                ReceivedMqttPacket receivedMqttPacket;
                if (timeout > TimeSpan.Zero)
                {
                    receivedMqttPacket = await ReceiveAsync(_channel.RawReceiveStream).TimeoutAfter(timeout).ConfigureAwait(false);
                }
                else
                {
                    receivedMqttPacket = await ReceiveAsync(_channel.ReceiveStream).ConfigureAwait(false);
                }

                var packet = PacketSerializer.Deserialize(receivedMqttPacket);
                if (packet == null)
                {
                    throw new MqttProtocolViolationException("Received malformed packet.");
                }

                MqttTrace.Information(nameof(MqttChannelCommunicationAdapter), "RX <<< {0}", packet);
                return packet;
            }
            catch (MqttCommunicationTimedOutException)
            {
                throw;
            }
            catch (MqttCommunicationException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new MqttCommunicationException(exception);
            }
        }

        private static async Task<ReceivedMqttPacket> ReceiveAsync(Stream stream)
        {
            var header = MqttPacketReader.ReadHeaderFromSource(stream);

            if (header.BodyLength == 0)
            {
                return new ReceivedMqttPacket(header, new MemoryStream(0));
            }

            var body = new byte[header.BodyLength];

            var offset = 0;
            do
            {
                var readBytesCount = await stream.ReadAsync(body, offset, body.Length - offset).ConfigureAwait(false);
                offset += readBytesCount;
            } while (offset < header.BodyLength);

            if (offset > header.BodyLength)
            {
                throw new MqttCommunicationException($"Read more body bytes than required ({offset}/{header.BodyLength}).");
            }

            return new ReceivedMqttPacket(header, new MemoryStream(body, 0, body.Length));
        }
    }
}