using System;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Channel
{
    public interface IMqttChannel : IDisposable
    {
        Task ConnectAsync(CancellationToken cancellationToken);
        Task DisconnectAsync();

        Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);
        Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);
    }
}
