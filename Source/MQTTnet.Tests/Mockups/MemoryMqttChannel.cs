// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Channel;
using MQTTnet.Internal;
using System.Buffers;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Tests.Mockups;

public sealed class MemoryMqttChannel : IMqttChannel
{
    readonly MemoryStream _stream;

    public MemoryMqttChannel(MemoryStream stream)
    {
        _stream = stream;
    }

    public MemoryMqttChannel(byte[] buffer)
    {
        _stream = new MemoryStream(buffer);
    }

    public EndPoint RemoteEndPoint { get; set; }

    public bool IsSecureConnection { get; } = false;

    public X509Certificate2 ClientCertificate { get; set; }

    public Task ConnectAsync(CancellationToken cancellationToken)
    {
        return CompletedTask.Instance;
    }

    public Task DisconnectAsync(CancellationToken cancellationToken)
    {
        return CompletedTask.Instance;
    }

    public Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return _stream.ReadAsync(buffer, offset, count, cancellationToken);
    }

    public async Task WriteAsync(ReadOnlySequence<byte> buffer, bool isEndOfPacket, CancellationToken cancellationToken)
    {
        foreach (var segment in buffer)
        {
            await _stream.WriteAsync(segment, cancellationToken).ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
    }
}