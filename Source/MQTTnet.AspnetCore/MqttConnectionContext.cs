﻿using Microsoft.AspNetCore.Connections;
using MQTTnet.Adapter;
using MQTTnet.AspNetCore.Client.Tcp;
using MQTTnet.Packets;
using MQTTnet.Serializer;
using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore
{
    public class MqttConnectionContext : IMqttChannelAdapter
    {
        public MqttConnectionContext(
            IMqttPacketSerializer packetSerializer,
            ConnectionContext connection)
        {
            PacketSerializer = packetSerializer;
            Connection = connection;
        }

        public string Endpoint => Connection.ConnectionId;
        public ConnectionContext Connection { get; }
        public IMqttPacketSerializer PacketSerializer { get; }
        public event EventHandler ReadingPacketStarted;
        public event EventHandler ReadingPacketCompleted;

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
                            else
                            {
                                // we did receive something but the message is not yet complete
                                ReadingPacketStarted?.Invoke(this, EventArgs.Empty);
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

        public Task SendPacketAsync(MqttBasePacket packet, CancellationToken cancellationToken)
        {
            var buffer = PacketSerializer.Serialize(packet);
            return Connection.Transport.Output.WriteAsync(buffer.AsMemory(), cancellationToken).AsTask();
        }

        public void Dispose()
        {
        }
    }
}
