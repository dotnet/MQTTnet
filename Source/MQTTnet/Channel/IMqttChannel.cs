// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Channel
{
    public interface IMqttChannel : IDisposable
    {
        string Endpoint { get; }
        
        bool IsSecureConnection { get; }
        
        X509Certificate2 ClientCertificate { get; }

        Task ConnectAsync(CancellationToken cancellationToken);
        
        Task DisconnectAsync(CancellationToken cancellationToken);

        Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);
        
        Task WriteAsync(ArraySegment<byte> buffer, bool isEndOfPacket, CancellationToken cancellationToken);
    }
}
