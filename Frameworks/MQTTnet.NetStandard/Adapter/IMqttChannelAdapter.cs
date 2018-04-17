﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Packets;
using MQTTnet.Serializer;

namespace MQTTnet.Adapter
{
    public interface IMqttChannelAdapter : IDisposable
    {
        IMqttPacketSerializer PacketSerializer { get; }

        Task ConnectAsync(TimeSpan timeout);

        Task DisconnectAsync(TimeSpan timeout);

        Task SendPacketsAsync(TimeSpan timeout, CancellationToken cancellationToken, MqttBasePacket[] packets);

        Task<MqttBasePacket> ReceivePacketAsync(TimeSpan timeout, CancellationToken cancellationToken);
    }
}
