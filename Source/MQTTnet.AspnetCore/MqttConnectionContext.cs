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
            
            _reader = new SpanBasedMqttPacketBodyReader();
            _received = new ReceivedMqttPacket(0, _reader, 0);

            if (Connection.Transport != null)
            {
                _input = Connection.Transport.Input;
                _output = Connection.Transport.Output;
            }
        }

        private PipeReader _input;
        private PipeWriter _output;
        private readonly SpanBasedMqttPacketBodyReader _reader;
        private readonly ReceivedMqttPacket _received;

        public string Endpoint => Connection.ConnectionId;
        public ConnectionContext Connection { get; }
        public MqttPacketFormatterAdapter PacketFormatterAdapter { get; }
        public event EventHandler ReadingPacketStarted;
        public event EventHandler<MqttBasePacket> ReadingPacketCompleted;

        private readonly SemaphoreSlim _writerSemaphore = new SemaphoreSlim(1, 1);

        public async Task ConnectAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (Connection is TcpConnection tcp && !tcp.IsConnected)
            {
                await tcp.StartAsync();
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
                        if (PacketFormatterAdapter.TryDecode(_reader, _received, buffer, out var packet, out consumed, out observed))
                        {
                            ReadingPacketCompleted?.Invoke(this, packet);
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

            cancellationToken.ThrowIfCancellationRequested();
            return null;
        }

        public async Task SendPacketAsync(MqttBasePacket packet, CancellationToken cancellationToken)
        {
            var buffer = PacketFormatterAdapter.Encode(packet);
            var msg = buffer.AsMemory();

            await _writerSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var output = _output;
                msg.CopyTo(output.GetMemory(msg.Length));
                output.Advance(msg.Length);
                await output.FlushAsync().ConfigureAwait(false);
            }
            finally
            {
                _writerSemaphore.Release();
            }
        }

        public void Dispose()
        {
        }

        public async Task ReceivePacketAsync(CancellationToken cancellationToken)
        {
            var input = _input;
            var reader = _reader;
            var received = _received;
            var formatter = PacketFormatterAdapter.Formatter;

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
                    readResult = await readTask;
                }

                var buffer = readResult.Buffer;

                var consumed = buffer.Start;
                var observed = buffer.Start;

                try
                {
                    if (!buffer.IsEmpty)
                    {
                        while (formatter.TryDecode(reader, received, buffer, out var packet, out consumed, out observed))
                        {
                            ReadingPacketCompleted?.Invoke(this, packet);
                            buffer = buffer.Slice(consumed);
                        }

                        // we did receive something but the message is not yet complete
                        ReadingPacketStarted?.Invoke(this, EventArgs.Empty);
                    }
                    else if (readResult.IsCompleted)
                    {
                        break;
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
    }
}
