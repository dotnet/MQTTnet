// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace MQTTnet.Channel;

public interface IMqttChannel : IDisposable
{
    X509Certificate2 ClientCertificate { get; }

    EndPoint RemoteEndPoint { get; }

    EndPoint LocalEndPoint { get; }

    bool IsSecureConnection { get; }

    Task ConnectAsync(CancellationToken cancellationToken);

    Task DisconnectAsync(CancellationToken cancellationToken);

    Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);

    Task WriteAsync(ReadOnlySequence<byte> buffer, bool isEndOfPacket, CancellationToken cancellationToken);
}