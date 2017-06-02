using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using MQTTnet.Core.Channel;
using MQTTnet.Core.Client;
using MQTTnet.Core.Exceptions;

namespace MQTTnet
{
    public class MqttTcpChannel : IMqttCommunicationChannel, IDisposable
    {
        private readonly Socket _socket;

        public MqttTcpChannel()
        {
            _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        }

        public MqttTcpChannel(Socket socket)
        {
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));
        }

        public Task ConnectAsync(MqttClientOptions options)
        {
            try
            {
                return Task.Factory.FromAsync(_socket.BeginConnect, _socket.EndConnect, options.Server, options.Port, null);
            }
            catch (SocketException exception)
            {
                throw new MqttCommunicationException(exception);
            }
        }

        public Task DisconnectAsync()
        {
            try
            {
                return Task.Factory.FromAsync(_socket.BeginDisconnect, _socket.EndDisconnect, true, null);
            }
            catch (SocketException exception)
            {
                throw new MqttCommunicationException(exception);
            }
        }

        public Task WriteAsync(byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            try
            {
                return Task.Factory.FromAsync(
                    // ReSharper disable once AssignNullToNotNullAttribute
                    _socket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, null, null),
                    _socket.EndSend);
            }
            catch (SocketException exception)
            {
                throw new MqttCommunicationException(exception);
            }
        }

        public Task ReadAsync(byte[] buffer)
        {
            try
            {
                return Task.Factory.FromAsync(
                    // ReSharper disable once AssignNullToNotNullAttribute
                    _socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, null, null),
                    _socket.EndReceive);
            }
            catch (SocketException exception)
            {
                throw new MqttCommunicationException(exception);
            }
        }

        public void Dispose()
        {
            _socket?.Dispose();
        }
    }
}