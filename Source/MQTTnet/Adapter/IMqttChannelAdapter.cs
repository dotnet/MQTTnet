// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Formatter;
using MQTTnet.Packets;

namespace MQTTnet.Adapter;

public interface IMqttChannelAdapter : IDisposable
{
    long BytesReceived { get; }

    long BytesSent { get; }

    X509Certificate2 ClientCertificate { get; }
    string Endpoint { get; }

    bool IsSecureConnection { get; }

    MqttPacketFormatterAdapter PacketFormatterAdapter { get; }

    Task ConnectAsync(CancellationToken cancellationToken);

    Task DisconnectAsync(CancellationToken cancellationToken);

    Task<MqttPacket> ReceivePacketAsync(CancellationToken cancellationToken);

    void ResetStatistics();

    Task SendPacketAsync(MqttPacket packet, CancellationToken cancellationToken);
}