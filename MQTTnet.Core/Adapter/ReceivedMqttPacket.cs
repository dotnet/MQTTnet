﻿using System;
using System.IO;
using MQTTnet.Core.Packets;

namespace MQTTnet.Core.Adapter
{
    public sealed class ReceivedMqttPacket : IDisposable
    {
        public ReceivedMqttPacket(MqttPacketHeader header, MemoryStream body)
        {
            Header = header ?? throw new ArgumentNullException(nameof(header));
            Body = body ?? throw new ArgumentNullException(nameof(body));
        }

        public MqttPacketHeader Header { get; }

        public MemoryStream Body { get; }

        public void Dispose()
        {
            Body?.Dispose();
        }
    }
}
