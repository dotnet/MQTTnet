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
using MQTTnet.Server;

namespace MQTTnet.Tests.Server
{
    [TestClass]
    public sealed class Connection_Tests : BaseTestClass
    {
        [TestMethod]
        public async Task Close_Idle_Connection_On_Connect()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer(new MqttServerOptionsBuilder().WithDefaultCommunicationTimeout(TimeSpan.FromSeconds(1)));

                var client = new CrossPlatformSocket(AddressFamily.InterNetwork, ProtocolType.Tcp);
                await client.ConnectAsync("localhost", testEnvironment.ServerPort, CancellationToken.None);

                // Don't send anything. The server should close the connection.
                await Task.Delay(TimeSpan.FromSeconds(3));

                try
                {
                    var receivedBytes = await client.ReceiveAsync(new ArraySegment<byte>(new byte[10]), SocketFlags.Partial);
                    if (receivedBytes == 0)
                    {
                        return;
                    }

                    Assert.Fail("Receive should throw an exception.");
                }
                catch (SocketException)
                {
                }
            }
        }

        [TestMethod]
        public async Task Send_Garbage()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer(new MqttServerOptionsBuilder().WithDefaultCommunicationTimeout(TimeSpan.FromSeconds(1)));

                // Send an invalid packet and ensure that the server will close the connection and stay in a waiting state
                // forever. This is security related.
                var client = new CrossPlatformSocket(AddressFamily.InterNetwork, ProtocolType.Tcp);
                await client.ConnectAsync("localhost", testEnvironment.ServerPort, CancellationToken.None);

                var buffer = Encoding.UTF8.GetBytes("Garbage");
                await client.SendAsync(new ArraySegment<byte>(buffer), SocketFlags.None);

                await Task.Delay(TimeSpan.FromSeconds(3));

                try
                {
                    var receivedBytes = await client.ReceiveAsync(new ArraySegment<byte>(new byte[10]), SocketFlags.Partial);
                    if (receivedBytes == 0)
                    {
                        return;
                    }

                    Assert.Fail("Receive should throw an exception.");
                }
                catch (SocketException)
                {
                }
            }
        }
    }
}