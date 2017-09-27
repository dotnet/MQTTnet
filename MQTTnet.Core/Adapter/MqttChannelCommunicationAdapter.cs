using System;
using System.Collections.Concurrent;
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

        private readonly ConcurrentQueue<byte[]> _pendingMessageQueue = new ConcurrentQueue<byte[]>();
        private bool _isWritingToStream;

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
                byte[] message;

                using (var bufferStream = new MemoryStream())
                {
                    foreach (var packet in packets)
                    {
                        MqttTrace.Information(nameof(MqttChannelCommunicationAdapter), "TX >>> {0} [Timeout={1}]", packet, timeout);

                        var writeBuffer = PacketSerializer.Serialize(packet);

                        bufferStream.Write(writeBuffer, 0, writeBuffer.Length);
                    }
                    message = bufferStream.ToArray();
                }

                await QueuedAndWriteBufferToStreamAsync(_channel.SendStream, message, timeout, cancellationToken).ConfigureAwait(false);
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

        private async Task QueuedAndWriteBufferToStreamAsync(Stream stream, byte[] message, TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (message == null)
            {
                return;
            }

            _pendingMessageQueue.Enqueue(message);

            lock (_pendingMessageQueue)
            {
                if (_isWritingToStream)
                {
                    return;
                }
                _isWritingToStream = true;
            }

            try
            {

                if (_pendingMessageQueue.Count > 0 && _pendingMessageQueue.TryDequeue(out var messageFromQueue))
                {
                    await stream.WriteAsync(messageFromQueue, 0, messageFromQueue.Length, cancellationToken).ConfigureAwait(false);

                    if (timeout > TimeSpan.Zero)
                    {
                        await stream.FlushAsync(cancellationToken).TimeoutAfter(timeout).ConfigureAwait(false);
                    }
                    else
                    {
                        await stream.FlushAsync(cancellationToken);
                    }
                }
                else
                {
                    lock (_pendingMessageQueue)
                    {
                        _isWritingToStream = false;
                    }
                }
            }
            catch (Exception)
            {
                lock (_pendingMessageQueue)
                {
                    _isWritingToStream = false;
                }

                throw;
            }
            finally
            {
                lock (_pendingMessageQueue)
                {
                    _isWritingToStream = false;
                }
            }
        }
    }
}