using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Core.Channel;
using MQTTnet.Core.Diagnostics;
using MQTTnet.Core.Exceptions;
using MQTTnet.Core.Internal;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Serializer;

namespace MQTTnet.Core.Adapter
{
    public class MqttChannelCommunicationAdapter : IMqttCommunicationAdapter
    {
        private readonly MqttNetTrace _trace;
        private readonly IMqttCommunicationChannel _channel;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public MqttChannelCommunicationAdapter(IMqttCommunicationChannel channel, IMqttPacketSerializer serializer, MqttNetTrace trace)
        {
            _trace = trace ?? throw new ArgumentNullException(nameof(trace));
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
            PacketSerializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        public IMqttPacketSerializer PacketSerializer { get; }

        public async Task ConnectAsync(TimeSpan timeout)
        {
            try
            {
                await _channel.ConnectAsync().TimeoutAfter(timeout).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                throw;
            }
            catch (OperationCanceledException)
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
            catch (OperationCanceledException)
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
            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                foreach (var packet in packets)
                {
                    if (packet == null)
                    {
                        continue;
                    }

                    _trace.Information(nameof(MqttChannelCommunicationAdapter), "TX >>> {0} [Timeout={1}]", packet, timeout);

                    var writeBuffer = PacketSerializer.Serialize(packet);
                    await _channel.SendStream.WriteAsync(writeBuffer, 0, writeBuffer.Length, cancellationToken).ConfigureAwait(false);
                }

                if (timeout > TimeSpan.Zero)
                {
                    await _channel.SendStream.FlushAsync(cancellationToken).TimeoutAfter(timeout).ConfigureAwait(false);
                }
                else
                {
                    await _channel.SendStream.FlushAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            catch (TaskCanceledException)
            {
                throw;
            }
            catch (OperationCanceledException)
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
            finally
            {
                _semaphore.Release();
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

                _trace.Information(nameof(MqttChannelCommunicationAdapter), "RX <<< {0}", packet);
                return packet;
            }
            catch (TaskCanceledException)
            {
                throw;
            }
            catch (OperationCanceledException)
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
