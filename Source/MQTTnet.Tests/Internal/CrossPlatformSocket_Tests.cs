// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Implementations;
using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Tests.Internal
{
    [TestClass]
    public class CrossPlatformSocket_Tests
    {
        [TestMethod]
        public async Task Connect_Send_Receive()
        {
            var serverPort = GetServerPort();
            var responseContent = "Connect_Send_Receive";

            // create a localhost web server.
            var builder = WebApplication.CreateSlimBuilder();
            builder.WebHost.UseKestrel(k => k.ListenLocalhost(serverPort));

            await using var webApp = builder.Build();
            var webAppStartedSource = new TaskCompletionSource();
            webApp.Lifetime.ApplicationStarted.Register(() => webAppStartedSource.TrySetResult());
            webApp.Use(next => context => context.Response.WriteAsync(responseContent));
            await webApp.StartAsync();
            await webAppStartedSource.Task;


            var crossPlatformSocket = new CrossPlatformSocket(ProtocolType.Tcp);
            await crossPlatformSocket.ConnectAsync(new DnsEndPoint("localhost", serverPort), CancellationToken.None);

            var requestBuffer = Encoding.UTF8.GetBytes($"GET /test/path HTTP/1.1\r\nHost: localhost:{serverPort}\r\n\r\n");
            await crossPlatformSocket.SendAsync(new ArraySegment<byte>(requestBuffer), SocketFlags.None);

            var buffer = new byte[1024];
            var length = await crossPlatformSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
            crossPlatformSocket.Dispose();

            var responseText = Encoding.UTF8.GetString(buffer, 0, length);

            Assert.IsTrue(responseText.Contains(responseContent));


            static int GetServerPort(int defaultPort = 9999)
            {
                var listeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
                var portSet = listeners.Select(i => i.Port).ToHashSet();

                while (!portSet.Add(defaultPort))
                {
                    defaultPort += 1;
                }
                return defaultPort;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(OperationCanceledException))]
        public async Task Try_Connect_Invalid_Host()
        {
            var crossPlatformSocket = new CrossPlatformSocket(ProtocolType.Tcp);

            var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            cancellationToken.Token.Register(() => crossPlatformSocket.Dispose());

            await crossPlatformSocket.ConnectAsync(new DnsEndPoint("www.google.de", 54321), cancellationToken.Token);
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
