using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using MQTTnet.Core.Channel;
using MQTTnet.Core.Client;

namespace MQTTnet.NETFramework
{
    public class MqttTcpChannel : IMqttTransportChannel, IDisposable
    {
        private readonly Socket _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

        public async Task ConnectAsync(MqttClientOptions options)
        {
            await Task.Factory.FromAsync(_socket.BeginConnect, _socket.EndConnect, options.Server, options.Port, null);
        }

        public async Task DisconnectAsync()
        {
            await Task.Factory.FromAsync(_socket.BeginDisconnect, _socket.EndDisconnect, true, null);
        }

        public async Task WriteAsync(byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            await Task.Factory.FromAsync(
                // ReSharper disable once AssignNullToNotNullAttribute
                _socket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, null, null),
                _socket.EndSend);
        }

        public async Task ReadAsync(byte[] buffer)
        {
            await Task.Factory.FromAsync(
                // ReSharper disable once AssignNullToNotNullAttribute
                _socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, null, null),
                _socket.EndReceive);
        }

        public void Dispose()
        {
            _socket?.Dispose();
        }
    }
}