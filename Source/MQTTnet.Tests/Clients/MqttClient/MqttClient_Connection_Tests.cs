// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Exceptions;
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
        [ExpectedException(typeof(MqttCommunicationException))]
        public async Task Connect_To_Invalid_Server_Port_Not_Opened()
        {
            var client = new MqttClientFactory().CreateMqttClient();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1", 12345).Build(), timeout.Token);
        }

        [TestMethod]
        [ExpectedException(typeof(OperationCanceledException))]
        public async Task Connect_To_Invalid_Server_Wrong_IP()
        {
            var client = new MqttClientFactory().CreateMqttClient();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("1.2.3.4").Build(), timeout.Token);
        }

        [TestMethod]
        [ExpectedException(typeof(MqttCommunicationException))]
        public async Task Connect_To_Invalid_Server_Wrong_Protocol()
        {
            var client = new MqttClientFactory().CreateMqttClient();
            await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("http://127.0.0.1", 12345).WithTimeout(TimeSpan.FromSeconds(2)).Build());
        }

        [TestMethod]
        public async Task ConnectTimeout_Throws_Exception()
        {
            var factory = new MqttClientFactory();
            using var client = factory.CreateMqttClient();
            var disconnectHandlerCalled = false;
            try
            {
                client.DisconnectedAsync += _ =>
                {
                    disconnectHandlerCalled = true;
                    return CompletedTask.Instance;
                };

                await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1").Build());

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

        [TestMethod]
        public async Task Disconnect_Clean()
        {
            using var testEnvironment = CreateTestEnvironment(MqttProtocolVersion.V500);
            var server = await testEnvironment.StartServer();

            ClientDisconnectedEventArgs eventArgs = null;
            server.ClientDisconnectedAsync += args =>
            {
                eventArgs = args;
                return CompletedTask.Instance;
            };

            var client = await testEnvironment.ConnectClient();

            var disconnectOptions = testEnvironment.ClientFactory.CreateClientDisconnectOptionsBuilder().WithReason(MqttClientDisconnectOptionsReason.MessageRateTooHigh).Build();

            // Perform a clean disconnect.
            await client.DisconnectAsync(disconnectOptions);

            await LongTestDelay();

            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(MqttClientDisconnectType.Clean, eventArgs.DisconnectType);
        }

        [TestMethod]
        public async Task Disconnect_Clean_With_Custom_Reason()
        {
            using var testEnvironment = CreateTestEnvironment(MqttProtocolVersion.V500);
            var server = await testEnvironment.StartServer();

            ClientDisconnectedEventArgs eventArgs = null;
            server.ClientDisconnectedAsync += args =>
            {
                eventArgs = args;
                return CompletedTask.Instance;
            };

            var client = await testEnvironment.ConnectClient();

            var disconnectOptions = testEnvironment.ClientFactory.CreateClientDisconnectOptionsBuilder().WithReason(MqttClientDisconnectOptionsReason.MessageRateTooHigh).Build();

            // Perform a clean disconnect.
            await client.DisconnectAsync(disconnectOptions);

            await LongTestDelay();

            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(MqttDisconnectReasonCode.MessageRateTooHigh, eventArgs.ReasonCode);
        }

        [TestMethod]
        public async Task Disconnect_Clean_With_User_Properties()
        {
            using var testEnvironment = CreateTestEnvironment(MqttProtocolVersion.V500);
            var server = await testEnvironment.StartServer();

            ClientDisconnectedEventArgs eventArgs = null;
            server.ClientDisconnectedAsync += args =>
            {
                eventArgs = args;
                return CompletedTask.Instance;
            };

            var client = await testEnvironment.ConnectClient();

            var disconnectOptions = testEnvironment.ClientFactory.CreateClientDisconnectOptionsBuilder().WithUserProperty("test_name", "test_value").Build();

            // Perform a clean disconnect.
            await client.DisconnectAsync(disconnectOptions);

            await LongTestDelay();

            Assert.IsNotNull(eventArgs);
            Assert.IsNotNull(eventArgs.UserProperties);
            Assert.AreEqual(1, eventArgs.UserProperties.Count);
            Assert.AreEqual("test_name", eventArgs.UserProperties[0].Name);
            Assert.AreEqual("test_value", eventArgs.UserProperties[0].Value);
        }

        class TestClientKerberosAuthenticationHandler : IMqttEnhancedAuthenticationHandler
        {
            public async Task HandleEnhancedAuthenticationAsync(MqttEnhancedAuthenticationEventArgs eventArgs)
            {
                if (eventArgs.AuthenticationMethod != "GS2-KRB5")
                {
                    throw new InvalidOperationException("Wrong authentication method");
                }

                var sendOptions = new SendMqttEnhancedAuthenticationDataOptions
                {
                    Data = "initial context token"u8.ToArray()
                };

                await eventArgs.SendAsync(sendOptions, eventArgs.CancellationToken);

                var response = await eventArgs.ReceiveAsync(eventArgs.CancellationToken);

                Assert.AreEqual(Encoding.UTF8.GetString(response.AuthenticationData), "reply context token");

                // No further data is required, but we have to fulfil the exchange.
                sendOptions = new SendMqttEnhancedAuthenticationDataOptions
                {
                    Data = []
                };

                await eventArgs.SendAsync(sendOptions, eventArgs.CancellationToken);
            }
        }

        [TestMethod]
        public async Task Use_Enhanced_Authentication()
        {
            using var testEnvironment = CreateTestEnvironment(MqttProtocolVersion.V500);
            var server = await testEnvironment.StartServer();

            server.ValidatingConnectionAsync += async args =>
            {
                if (args.AuthenticationMethod == "GS2-KRB5")
                {
                    var result = await args.ExchangeEnhancedAuthenticationAsync(null, args.CancellationToken);

                    Assert.AreEqual(Encoding.UTF8.GetString(result.AuthenticationData), "initial context token");

                    var authOptions = testEnvironment.ServerFactory.CreateExchangeExtendedAuthenticationOptionsBuilder().WithAuthenticationData("reply context token").Build();

                    result =  await args.ExchangeEnhancedAuthenticationAsync(authOptions, args.CancellationToken);

                    Assert.AreEqual(Encoding.UTF8.GetString(result.AuthenticationData), "");

                    args.ResponseAuthenticationData = "outcome of authentication"u8.ToArray();
                }
                else
                {
                    args.ReasonCode = MqttConnectReasonCode.BadAuthenticationMethod;
                }
            };

            // Use Kerberos sample from the MQTT RFC.
            var kerberosAuthenticationHandler = new TestClientKerberosAuthenticationHandler();

            var clientOptions = testEnvironment.CreateDefaultClientOptionsBuilder().WithAuthentication("GS2-KRB5").WithEnhancedAuthenticationHandler(kerberosAuthenticationHandler);
            var client = await testEnvironment.ConnectClient(clientOptions);

            Assert.IsTrue(client.IsConnected);
        }

        [TestMethod]
        public async Task No_Unobserved_Exception()
        {
            using var testEnvironment = CreateTestEnvironment();
            testEnvironment.IgnoreClientLogErrors = true;

            var client = testEnvironment.CreateClient();
            var options = new MqttClientOptionsBuilder().WithTcpServer("1.2.3.4").WithTimeout(TimeSpan.FromSeconds(2)).Build();

            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(0.5));
                await client.ConnectAsync(options, timeout.Token);
            }
            catch (OperationCanceledException)
            {
            }

            client.Dispose();

            // These delays and GC calls are required in order to make calling the finalizer reproducible.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            await LongTestDelay();
            await LongTestDelay();
            await LongTestDelay();
        }

        [TestMethod]
        public async Task Return_Non_Success()
        {
            using var testEnvironment = CreateTestEnvironment(MqttProtocolVersion.V500);
            var server = await testEnvironment.StartServer();

            server.ValidatingConnectionAsync += args =>
            {
                args.ResponseUserProperties = [new("Property", "Value")];

                args.ReasonCode = MqttConnectReasonCode.QuotaExceeded;

                return CompletedTask.Instance;
            };

            var client = testEnvironment.CreateClient();

            var response = await client.ConnectAsync(testEnvironment.CreateDefaultClientOptionsBuilder().Build());

            Assert.IsNotNull(response);
            Assert.AreEqual(MqttClientConnectResultCode.QuotaExceeded, response.ResultCode);
            Assert.AreEqual(response.UserProperties[0].Name, "Property");
            Assert.AreEqual(response.UserProperties[0].Value, "Value");
        }

        [TestMethod]
        public async Task Throw_Proper_Exception_When_Not_Connected()
        {
            try
            {
                var mqttFactory = new MqttClientFactory();
                using var mqttClient = mqttFactory.CreateMqttClient();
                await mqttClient.SubscribeAsync("test", MqttQualityOfServiceLevel.AtLeastOnce);
            }
            catch (MqttClientNotConnectedException exception)
            {
                if (exception.Message == "The MQTT client is not connected.")
                {
                    return;
                }
            }

            Assert.Fail();
        }
    }
}