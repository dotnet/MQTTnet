using Microsoft.AspNetCore.Connections;
using MQTTnet.Adapter;
using MQTTnet.AspNetCore.Client.Tcp;
using MQTTnet.Packets;
using MQTTnet.Serializer;
using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore
{
    public class MqttConnectionContext : IMqttChannelAdapter
    {
        public IMqttPacketSerializer PacketSerializer { get; }
        public ConnectionContext Connection { get; }

        public string Endpoint => Connection.ConnectionId;

        public MqttConnectionContext(
            IMqttPacketSerializer packetSerializer,
            ConnectionContext connection)
        {
            PacketSerializer = packetSerializer;
            Connection = connection;
        }

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

        public void Dispose()
        {
        }

        public async Task<MqttBasePacket> ReceivePacketAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            var input = Connection.Transport.Input;

            while (!cancellationToken.IsCancellationRequested)
            {
                ReadResult readResult;
                ReadingPacketStarted?.Invoke(this, EventArgs.Empty);

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
                        if (PacketSerializer.TryDeserialize(buffer, out var packet, out consumed, out observed))
                        {
                            return packet;
                        }
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
                    ReadingPacketCompleted?.Invoke(this, EventArgs.Empty);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            return null;
        }       

        public async Task SendPacketsAsync(TimeSpan timeout, IEnumerable<MqttBasePacket> packets, CancellationToken cancellationToken)
        {
            foreach (var packet in packets)
            {
                var buffer = PacketSerializer.Serialize(packet);
                await Connection.Transport.Output.WriteAsync(buffer.AsMemory());
            }
        }
        
        public event EventHandler ReadingPacketStarted;
        public event EventHandler ReadingPacketCompleted;
    }
}
