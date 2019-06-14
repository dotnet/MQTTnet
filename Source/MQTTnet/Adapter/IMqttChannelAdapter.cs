﻿using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Formatter;
using MQTTnet.Packets;

namespace MQTTnet.Adapter
{
    public interface IMqttChannelAdapter : IDisposable
    {
        string Endpoint { get; }

        bool IsSecureConnection { get; }

        X509Certificate2 ClientCertificate { get; }

        MqttPacketFormatterAdapter PacketFormatterAdapter { get; }

        long BytesSent { get; }

        long BytesReceived { get; }

        Action ReadingPacketStartedCallback { get; set; }

        Action ReadingPacketCompletedCallback { get; set; }

        Task ConnectAsync(TimeSpan timeout, CancellationToken cancellationToken);

        Task DisconnectAsync(TimeSpan timeout, CancellationToken cancellationToken);

        Task SendPacketAsync(MqttBasePacket packet, TimeSpan timeout, CancellationToken cancellationToken);

        Task<MqttBasePacket> ReceivePacketAsync(TimeSpan timeout, CancellationToken cancellationToken);

        void ResetStatistics();
    }
}
