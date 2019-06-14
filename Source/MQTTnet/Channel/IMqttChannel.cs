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
        Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);
    }
}
