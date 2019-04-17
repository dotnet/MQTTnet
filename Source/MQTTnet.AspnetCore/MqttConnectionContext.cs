using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections.Features;
using MQTTnet.Adapter;
using MQTTnet.AspNetCore.Client.Tcp;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

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


            _reader = new SpanBasedMqttPacketBodyReader();
        }

        private PipeReader _input;
        private PipeWriter _output;
        private readonly SpanBasedMqttPacketBodyReader _reader;

        public string Endpoint 
        {
            get {
                var connection = Http?.HttpContext?.Connection;
                if (connection == null)
                {
                    return Connection.ConnectionId;
                }

                return $"{connection.RemoteIpAddress}:{connection.RemotePort}";
            }
        }

        public bool IsSecureConnection => Http?.HttpContext?.Request?.IsHttps ?? false;

        private IHttpContextFeature Http => Connection.Features.Get<IHttpContextFeature>();

        public ConnectionContext Connection { get; }
        public MqttPacketFormatterAdapter PacketFormatterAdapter { get; }

        public long BytesSent { get; set; } 
        public long BytesReceived { get; set; }

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
                            if (PacketFormatterAdapter.TryDecode(_reader, buffer, out var packet, out consumed, out observed, out var received))
                            {
                                BytesReceived += received;
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
            var formatter = PacketFormatterAdapter;
           

            await _writerSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var buffer = formatter.Encode(packet);
                var msg = buffer.AsMemory();
                var output = _output;
                msg.CopyTo(output.GetMemory(msg.Length));
                BytesSent += msg.Length;
                PacketFormatterAdapter.FreeBuffer();
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
    }
}
