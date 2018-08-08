using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Channel;

namespace MQTTnet.Internal
{
    public class TestMqttChannel : IMqttChannel
    {
        private readonly MemoryStream _stream;

        public TestMqttChannel(MemoryStream stream)
        {
            _stream = stream;
        }

        public string Endpoint { get; } = "<Test channel>";

        public Task ConnectAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }

        public Task DisconnectAsync()
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
