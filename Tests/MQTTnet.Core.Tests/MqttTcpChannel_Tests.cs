using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Implementations;

namespace MQTTnet.Tests
{
    [TestClass]
    public class MqttTcpChannel_Tests
    {
        [TestMethod]
        public async Task Dispose_Channel_While_Used()
        {
            var ct = new CancellationTokenSource();
            var serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                serverSocket.Bind(new IPEndPoint(IPAddress.Any, 50001));
                serverSocket.Listen(0);

#pragma warning disable 4014
                Task.Run(async () =>
#pragma warning restore 4014
                {
                    while (!ct.IsCancellationRequested)
                    {
                        var client = await serverSocket.AcceptAsync();
                        var data = new byte[] { 128 };
                        await client.SendAsync(new ArraySegment<byte>(data), SocketFlags.None);
                    }
                }, ct.Token);

                var clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                await clientSocket.ConnectAsync(IPAddress.Loopback, 50001);

                await Task.Delay(100, ct.Token);

                var tcpChannel = new MqttTcpChannel(new NetworkStream(clientSocket, true), "test", null);

                var buffer = new byte[1];
                await tcpChannel.ReadAsync(buffer, 0, 1, ct.Token);

                Assert.AreEqual(128, buffer[0]);

                // This block should fail after dispose.
#pragma warning disable 4014
                Task.Run(() =>
#pragma warning restore 4014
                {
                    Task.Delay(200, ct.Token);
                    tcpChannel.Dispose();
                }, ct.Token);

                try
                {
                    await tcpChannel.ReadAsync(buffer, 0, 1, CancellationToken.None);
                }
                catch (Exception exception)
                {
                    Assert.IsInstanceOfType(exception, typeof(SocketException));
                    Assert.AreEqual(SocketError.OperationAborted, ((SocketException)exception).SocketErrorCode);
                }
            }
            finally
            {
                ct.Cancel(false);
                serverSocket.Dispose();
            }
        }
    }
}
