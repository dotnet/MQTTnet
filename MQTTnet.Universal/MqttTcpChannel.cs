using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using MQTTnet.Core.Channel;
using MQTTnet.Core.Client;
using Buffer = Windows.Storage.Streams.Buffer;

namespace MQTTnet.Universal
{
    public sealed class MqttTcpChannel : IMqttTransportChannel, IDisposable
    {
        private readonly StreamSocket _socket = new StreamSocket();

        public async Task ConnectAsync(MqttClientOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            await _socket.ConnectAsync(new HostName(options.Server), options.Port.ToString());
        }

        public async Task DisconnectAsync()
        {
            await _socket.CancelIOAsync();
            _socket.Dispose();
        }

        public async Task WriteAsync(byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            await _socket.OutputStream.WriteAsync(buffer.AsBuffer());
            await _socket.OutputStream.FlushAsync();
        }

        public async Task ReadAsync(byte[] buffer)
        {
            var buffer2 = new Buffer((uint)buffer.Length);
            await _socket.InputStream.ReadAsync(buffer2, (uint)buffer.Length, InputStreamOptions.None);

            var array2 = buffer2.ToArray();
            Array.Copy(array2, buffer, array2.Length);
        }

        public void Dispose()
        {
            _socket?.Dispose();
        }
    }
}