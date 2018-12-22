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

            if (Connection.Transport != null)
            {
                _input = Connection.Transport.Input;
                _output = Connection.Transport.Output;
            }
        }
        private PipeReader _input;
        private PipeWriter _output;

        public string Endpoint => Connection.ConnectionId;
        public bool IsSecureConnection => false; // TODO: Fix detection (WS vs. WSS).

        public ConnectionContext Connection { get; }
        public MqttPacketFormatterAdapter PacketFormatterAdapter { get; }

        public long BytesSent { get; } // TODO: Fix calculation.
        public long BytesReceived { get; } // TODO: Fix calculation.

        public Action ReadingPacketStartedCallback { get; set; }
        public Action ReadingPacketCompletedCallback { get; set; }
        
        private readonly SemaphoreSlim _writerSemaphore = new SemaphoreSlim(1, 1);

        public async Task ConnectAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (Connection is TcpConnection tcp && !tcp.IsConnected)
            {
                await tcp.StartAsync().ConfigureAwait(false);
            }

            _input = Connection.Transport.Input;
            _output = Connection.Transport.Output;
        }

        public Task DisconnectAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            _input?.Complete();
            _output?.Complete();

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
                                ReadingPacketStartedCallback?.Invoke();
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
                ReadingPacketCompletedCallback?.Invoke();
            }

            cancellationToken.ThrowIfCancellationRequested();
            return null;
        }

        public async Task SendPacketAsync(MqttBasePacket packet, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var buffer = PacketFormatterAdapter.Encode(packet);
            var msg = buffer.AsMemory();
            var output = _output;

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
