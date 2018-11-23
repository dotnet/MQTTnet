using Microsoft.AspNetCore.Connections;
using MQTTnet.Adapter;
using MQTTnet.AspNetCore.Client.Tcp;
using MQTTnet.Exceptions;
using MQTTnet.Packets;
using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Formatter;

namespace MQTTnet.AspNetCore
{
    public class MqttConnectionContext : IMqttChannelAdapter
    {
        public MqttConnectionContext(MqttPacketFormatterAdapter packetFormatterAdapter, ConnectionContext connection)
        {
            PacketFormatterAdapter = packetFormatterAdapter ?? throw new ArgumentNullException(nameof(packetFormatterAdapter));
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        public string Endpoint => Connection.ConnectionId;
        public ConnectionContext Connection { get; }
        public MqttPacketFormatterAdapter PacketFormatterAdapter { get; }
        public event EventHandler ReadingPacketStarted;
        public event EventHandler ReadingPacketCompleted;

        private readonly SemaphoreSlim _writerSemaphore = new SemaphoreSlim(1, 1);

        public Task ConnectAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (Connection is TcpConnection tcp && !tcp.IsConnected)
            {
                return tcp.StartAsync();
            }
            return Task.CompletedTask;
        }

        public Task DisconnectAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            Connection.Transport.Input.Complete();
            Connection.Transport.Output.Complete();

            return Task.CompletedTask;
        }

        public async Task<MqttBasePacket> ReceivePacketAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            var input = Connection.Transport.Input;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    ReadResult readResult;
                    var readTask = input.ReadAsync(cancellationToken);
                    if (readTask.IsCompleted)
                    {
                        readResult = readTask.Result;
                    }
                    else
                    {
                        readResult = await readTask.ConfigureAwait(false);
                    }

                    var buffer = readResult.Buffer;

                    var consumed = buffer.Start;
                    var observed = buffer.Start;

                    try
                    {
                        if (!buffer.IsEmpty)
                        {
                            if (PacketFormatterAdapter.TryDecode(buffer, out var packet, out consumed, out observed))
                            {
                                return packet;
                            }
                            else
                            {
                                // we did receive something but the message is not yet complete
                                ReadingPacketStarted?.Invoke(this, EventArgs.Empty);
                            }
                        }
                        else if (readResult.IsCompleted)
                        {
                            throw new MqttCommunicationException("Connection Aborted");
                        }
                    }
                    finally
                    {
                        // The buffer was sliced up to where it was consumed, so we can just advance to the start.
                        // We mark examined as buffer.End so that if we didn't receive a full frame, we'll wait for more data
                        // before yielding the read again.
                        input.AdvanceTo(consumed, observed);
                    }
                }
            }
            finally
            {
                ReadingPacketCompleted?.Invoke(this, EventArgs.Empty);
            }

            cancellationToken.ThrowIfCancellationRequested();
            return null;
        }

        public async Task SendPacketAsync(MqttBasePacket packet, CancellationToken cancellationToken)
        {
            var buffer = PacketFormatterAdapter.Encode(packet).AsMemory();
            var output = Connection.Transport.Output;

            await _writerSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await output.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _writerSemaphore.Release();
            }
        }

        public void Dispose()
        {
        }
    }
}
