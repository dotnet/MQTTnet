// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Channel;

namespace MQTTnet.Tests.Mockups
{
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

        public string Endpoint { get; } = "<Test channel>";

        public bool IsSecureConnection { get; } = false;

        public X509Certificate2 ClientCertificate { get; }

        public Task ConnectAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }

        public Task DisconnectAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }

        public Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _stream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _stream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public void Dispose()
        {
        }
    }
}
