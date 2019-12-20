using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MQTTnet.Implementations
{
    public static class PlatformAbstractionLayer
    {
        public static async Task<Socket> AcceptAsync(Socket socket)
        {
#if NET452 || NET461
            try
            {
                return await Task.Factory.FromAsync(socket.BeginAccept, socket.EndAccept, null).ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                return null;
            }
#else
            return await socket.AcceptAsync().ConfigureAwait(false);
#endif
        }


        public static Task ConnectAsync(Socket socket, IPAddress ip, int port)
        {
#if NET452 || NET461
            return Task.Factory.FromAsync(socket.BeginConnect, socket.EndConnect, ip, port, null);
#else
            return socket.ConnectAsync(ip, port);
#endif
        }

        public static Task ConnectAsync(Socket socket, string host, int port)
        {
#if NET452 || NET461
            return Task.Factory.FromAsync(socket.BeginConnect, socket.EndConnect, host, port, null);
#else
            return socket.ConnectAsync(host, port);
#endif
        }

#if NET452 || NET461
        public class SocketWrapper 
        {
            private readonly Socket _socket;
            private readonly ArraySegment<byte> _buffer;
            private readonly SocketFlags _socketFlags;

            public SocketWrapper(Socket socket, ArraySegment<byte> buffer, SocketFlags socketFlags)
            {
                _socket = socket;
                _buffer = buffer;
                _socketFlags = socketFlags;
            }

            public static IAsyncResult BeginSend(AsyncCallback callback, object state)
            {
                var real = (SocketWrapper)state;
                return real._socket.BeginSend(real._buffer.Array, real._buffer.Offset, real._buffer.Count, real._socketFlags, callback, state);
            }

            public static IAsyncResult BeginReceive(AsyncCallback callback, object state)
            {
                var real = (SocketWrapper)state;
                return real._socket.BeginReceive(real._buffer.Array, real._buffer.Offset, real._buffer.Count, real._socketFlags, callback, state);
            }
        }
#endif

        public static Task SendAsync(Socket socket, ArraySegment<byte> buffer, SocketFlags socketFlags)
        {
#if NET452 || NET461            
            return Task.Factory.FromAsync(SocketWrapper.BeginSend, socket.EndSend, new SocketWrapper(socket, buffer, socketFlags));
#else
            return socket.SendAsync(buffer, socketFlags);
#endif
        }

        public static Task<int> ReceiveAsync(Socket socket, ArraySegment<byte> buffer, SocketFlags socketFlags)
        {
#if NET452 || NET461
            return Task.Factory.FromAsync(SocketWrapper.BeginReceive, socket.EndReceive, new SocketWrapper(socket, buffer, socketFlags));
#else
            return socket.ReceiveAsync(buffer, socketFlags);
#endif
        }

    }
}
