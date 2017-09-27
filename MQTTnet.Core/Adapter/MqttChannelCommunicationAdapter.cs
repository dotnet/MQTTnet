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

        private readonly Task _waitTask = Task.FromResult(0); // this task is used to prevent overlapping write

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
                await _channel.ConnectAsync(options).TimeoutAfter(timeout).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                throw;
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
            catch (TaskCanceledException)
            {
                throw;
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

        public async Task SendPacketsAsync(TimeSpan timeout, CancellationToken cancellationToken, IEnumerable<MqttBasePacket> packets)
        {
            try
            {
                var waitTask = _waitTask;

                foreach (var packet in packets)
                {
                    if (packet == null) { continue; }

                    MqttTrace.Information(nameof(MqttChannelCommunicationAdapter), $"TX >>> {packet} [Timeout={timeout}]");

                    var writeBuffer = PacketSerializer.Serialize(packet);

                    waitTask = waitTask.ContinueWith(t => _channel.SendStream.WriteAsync(writeBuffer, 0, writeBuffer.Length, cancellationToken).ConfigureAwait(false), cancellationToken);
                }

                waitTask = waitTask.ContinueWith(t =>
                {
                    if (timeout > TimeSpan.Zero)
                    {
                        return _channel.SendStream.FlushAsync(cancellationToken).TimeoutAfter(timeout).ConfigureAwait(false);
                    }

                    return _channel.SendStream.FlushAsync(cancellationToken).ConfigureAwait(false);
                }, cancellationToken);

                await waitTask; // configure await false generates stackoverflow
            }
            catch (TaskCanceledException)
            {
                throw;
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

        public async Task<MqttBasePacket> ReceivePacketAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            try
            {
                ReceivedMqttPacket receivedMqttPacket;
                if (timeout > TimeSpan.Zero)
                {
                    receivedMqttPacket = await ReceiveAsync(_channel.RawReceiveStream, cancellationToken).TimeoutAfter(timeout).ConfigureAwait(false);
                }
                else
                {
                    receivedMqttPacket = await ReceiveAsync(_channel.ReceiveStream, cancellationToken).ConfigureAwait(false);
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    throw new TaskCanceledException();
                }

                var packet = PacketSerializer.Deserialize(receivedMqttPacket);
                if (packet == null)
                {
                    throw new MqttProtocolViolationException("Received malformed packet.");
                }

                MqttTrace.Information(nameof(MqttChannelCommunicationAdapter), "RX <<< {0}", packet);
                return packet;
            }
            catch (TaskCanceledException)
            {
                throw;
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

        private static async Task<ReceivedMqttPacket> ReceiveAsync(Stream stream, CancellationToken cancellationToken)
        {
            var header = MqttPacketReader.ReadHeaderFromSource(stream, cancellationToken);

            if (header.BodyLength == 0)
            {
                return new ReceivedMqttPacket(header, new MemoryStream(0));
            }

            var body = new byte[header.BodyLength];

            var offset = 0;
            do
            {
                var readBytesCount = await stream.ReadAsync(body, offset, body.Length - offset, cancellationToken).ConfigureAwait(false);
                offset += readBytesCount;
            } while (offset < header.BodyLength);

            return new ReceivedMqttPacket(header, new MemoryStream(body, 0, body.Length));
        }
    }
}
