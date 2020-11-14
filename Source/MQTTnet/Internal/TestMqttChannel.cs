using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Channel;

namespace MQTTnet.Internal
{
    public class TestMqttChannel : IMqttChannel
    {
        readonly MemoryStream _stream;

        public TestMqttChannel(MemoryStream stream)
        {
            _stream = stream;
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
