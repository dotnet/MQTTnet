// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Exceptions;
using MQTTnet.Extensions.Rpc;
using MQTTnet.Formatter;
using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace MQTTnet.Tests.Clients.MqttClient
{
    [TestClass]
    public sealed class MqttClient_Connection_Tests : BaseTestClass
    {
        [TestMethod]
        public async Task Connect_Disconnect_Connect()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer();

                var clientOptions = testEnvironment.CreateDefaultClientOptions();
                var client = testEnvironment.CreateClient();

                await client.ConnectAsync(clientOptions);
                await client.DisconnectAsync();
                await client.ConnectAsync(clientOptions);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task Connect_Multiple_Times_Should_Fail()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer();

                var clientOptions = testEnvironment.CreateDefaultClientOptions();
                var client = testEnvironment.CreateClient();

                await client.ConnectAsync(clientOptions);
                await client.ConnectAsync(clientOptions);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(MqttCommunicationException))]
        public async Task Connect_To_Invalid_Server_Port_Not_Opened()
        {
            var client = new MqttFactory().CreateMqttClient();
            using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
            {
                await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1", 12345).Build(), timeout.Token);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(OperationCanceledException))]
        public async Task Connect_To_Invalid_Server_Wrong_IP()
        {
            var client = new MqttFactory().CreateMqttClient();
            using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2)))
            {
                await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("1.2.3.4").Build(), timeout.Token);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(MqttCommunicationException))]
        public async Task Connect_To_Invalid_Server_Wrong_Protocol()
        {
            var client = new MqttFactory().CreateMqttClient();
            await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("http://127.0.0.1", 12345).WithTimeout(TimeSpan.FromSeconds(2)).Build());
        }

        [TestMethod]
        public async Task ConnectTimeout_Throws_Exception()
        {
            var factory = new MqttFactory();
            using (var client = factory.CreateMqttClient())
            {
                var disconnectHandlerCalled = false;
                try
                {
                    client.DisconnectedAsync += args =>
                    {
                        disconnectHandlerCalled = true;
                        return CompletedTask.Instance;
                    };

                    await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("1.2.3.4").Build());

                    Assert.Fail("Must fail!");
                }
                catch (Exception exception)
                {
                    Assert.IsNotNull(exception);
                    Assert.IsInstanceOfType(exception, typeof(MqttCommunicationException));
                }

                await LongTestDelay(); // disconnected handler is called async
                Assert.IsTrue(disconnectHandlerCalled);
            }
        }

        [TestMethod]
        public async Task Disconnect_Clean()
        {
            using (var testEnvironment = CreateTestEnvironment(MqttProtocolVersion.V500))
            {
                var server = await testEnvironment.StartServer();

                ClientDisconnectedEventArgs eventArgs = null;
                server.ClientDisconnectedAsync += args =>
                {
                    eventArgs = args;
                    return CompletedTask.Instance;
                };

                var client = await testEnvironment.ConnectClient();

                var disconnectOptions = testEnvironment.Factory.CreateClientDisconnectOptionsBuilder().WithReason(MqttClientDisconnectOptionsReason.MessageRateTooHigh).Build();

                // Perform a clean disconnect.
                await client.DisconnectAsync(disconnectOptions);

                await LongTestDelay();

                Assert.IsNotNull(eventArgs);
                Assert.AreEqual(MqttClientDisconnectType.Clean, eventArgs.DisconnectType);
            }
        }

        [TestMethod]
        public async Task Disconnect_Clean_With_Custom_Reason()
        {
            using (var testEnvironment = CreateTestEnvironment(MqttProtocolVersion.V500))
            {
                var server = await testEnvironment.StartServer();

                ClientDisconnectedEventArgs eventArgs = null;
                server.ClientDisconnectedAsync += args =>
                {
                    eventArgs = args;
                    return CompletedTask.Instance;
                };

                var client = await testEnvironment.ConnectClient();

                var disconnectOptions = testEnvironment.Factory.CreateClientDisconnectOptionsBuilder().WithReason(MqttClientDisconnectOptionsReason.MessageRateTooHigh).Build();

                // Perform a clean disconnect.
                await client.DisconnectAsync(disconnectOptions);

                await LongTestDelay();

                Assert.IsNotNull(eventArgs);
                Assert.AreEqual(MqttDisconnectReasonCode.MessageRateTooHigh, eventArgs.ReasonCode);
            }
        }

        [TestMethod]
        public async Task Disconnect_Clean_With_User_Properties()
        {
            using (var testEnvironment = CreateTestEnvironment(MqttProtocolVersion.V500))
            {
                var server = await testEnvironment.StartServer();

                ClientDisconnectedEventArgs eventArgs = null;
                server.ClientDisconnectedAsync += args =>
                {
                    eventArgs = args;
                    return CompletedTask.Instance;
                };

                var client = await testEnvironment.ConnectClient();

                var disconnectOptions = testEnvironment.Factory.CreateClientDisconnectOptionsBuilder().WithUserProperty("test_name", "test_value").Build();

                // Perform a clean disconnect.
                await client.DisconnectAsync(disconnectOptions);

                await LongTestDelay();

                Assert.IsNotNull(eventArgs);
                Assert.IsNotNull(eventArgs.UserProperties);
                Assert.AreEqual(1, eventArgs.UserProperties.Count);
                Assert.AreEqual("test_name", eventArgs.UserProperties[0].Name);
                Assert.AreEqual("test_value", eventArgs.UserProperties[0].Value);
            }
        }

        [TestMethod]
        public async Task Return_Non_Success()
        {
            using (var testEnvironment = CreateTestEnvironment(MqttProtocolVersion.V500))
            {
                var server = await testEnvironment.StartServer();

                server.ValidatingConnectionAsync += args =>
                {
                    args.ResponseUserProperties = new List<MqttUserProperty>
                    {
                        new MqttUserProperty("Property", "Value")
                    };

                    args.ReasonCode = MqttConnectReasonCode.QuotaExceeded;

                    return CompletedTask.Instance;
                };

                var client = testEnvironment.CreateClient();

                var response = await client.ConnectAsync(testEnvironment.CreateDefaultClientOptionsBuilder().WithoutThrowOnNonSuccessfulConnectResponse().Build());

                Assert.IsNotNull(response);
                Assert.AreEqual(MqttClientConnectResultCode.QuotaExceeded, response.ResultCode);
                Assert.AreEqual(response.UserProperties[0].Name, "Property");
                Assert.AreEqual(response.UserProperties[0].Value, "Value");
            }
        }

        [TestMethod]
        [ExpectedException(typeof(MqttClientDisconnectedException))]
        public async Task Rpc_Client_Not_Connected()
        {
            var factory = new MqttFactory();
            using (var client = factory.CreateMqttClient())
            {
                var rpcClient = factory.CreateMqttRpcClient(client);
                await rpcClient.ExecuteAsync("aaa", new byte[] { 1, 2, 3 }, MqttQualityOfServiceLevel.AtLeastOnce);
            }
        }
    }
}