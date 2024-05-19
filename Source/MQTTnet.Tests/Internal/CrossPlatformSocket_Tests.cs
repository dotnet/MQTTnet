// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Implementations;

namespace MQTTnet.Tests.Internal
{
    [TestClass]
    public class CrossPlatformSocket_Tests
    {
        [TestMethod]
        public async Task Connect_Send_Receive()
        {
            var crossPlatformSocket = new CrossPlatformSocket(ProtocolType.Tcp);
            await crossPlatformSocket.ConnectAsync("www.google.de", 80, CancellationToken.None);

            var requestBuffer = Encoding.UTF8.GetBytes("GET / HTTP/1.1\r\nHost: www.google.de\r\n\r\n");
            await crossPlatformSocket.SendAsync(new ArraySegment<byte>(requestBuffer), System.Net.Sockets.SocketFlags.None);

            var buffer = new byte[1024];
            var length = await crossPlatformSocket.ReceiveAsync(new ArraySegment<byte>(buffer), System.Net.Sockets.SocketFlags.None);
            crossPlatformSocket.Dispose();

            var responseText = Encoding.UTF8.GetString(buffer, 0, length);

            Assert.IsTrue(responseText.Contains("HTTP/1.1 200 OK"));
        }

        [TestMethod]
        [ExpectedException(typeof(OperationCanceledException))]
        public async Task Try_Connect_Invalid_Host()
        {
            var crossPlatformSocket = new CrossPlatformSocket(ProtocolType.Tcp);

            var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            cancellationToken.Token.Register(() => crossPlatformSocket.Dispose());

            await crossPlatformSocket.ConnectAsync("www.google.de", 54321, cancellationToken.Token);
        }

        //[TestMethod]
        //public async Task Use_Disconnected_Socket()
        //{
        //    var crossPlatformSocket = new CrossPlatformSocket();

        //    await crossPlatformSocket.ConnectAsync("www.google.de", 80);

        //    var requestBuffer = Encoding.UTF8.GetBytes("GET /wrong_uri HTTP/1.1\r\nConnection: close\r\n\r\n");
        //    await crossPlatformSocket.SendAsync(new ArraySegment<byte>(requestBuffer), System.Net.Sockets.SocketFlags.None);

        //    var buffer = new byte[64000];
        //    var length = await crossPlatformSocket.ReceiveAsync(new ArraySegment<byte>(buffer), System.Net.Sockets.SocketFlags.None);

        //    await Task.Delay(500);

        //    await crossPlatformSocket.SendAsync(new ArraySegment<byte>(requestBuffer), System.Net.Sockets.SocketFlags.None);
        //}

        [TestMethod]
        public void Set_Options()
        {
            var crossPlatformSocket = new CrossPlatformSocket(ProtocolType.Tcp);

            Assert.IsFalse(crossPlatformSocket.ReuseAddress);
            crossPlatformSocket.ReuseAddress = true;
            Assert.IsTrue(crossPlatformSocket.ReuseAddress);

            Assert.IsFalse(crossPlatformSocket.NoDelay);
            crossPlatformSocket.NoDelay = true;
            Assert.IsTrue(crossPlatformSocket.NoDelay);
        }
    }
}
