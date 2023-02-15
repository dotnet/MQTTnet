// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Adapter;
using MQTTnet.Client;
using MQTTnet.Exceptions;
using MQTTnet.Internal;
using MQTTnet.Protocol;

namespace MQTTnet.Tests.Server
{
    [TestClass]
    public sealed class Security_Tests : BaseTestClass
    {
        [TestMethod]
        public async Task Do_Not_Affect_Authorized_Clients()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                testEnvironment.IgnoreClientLogErrors = true;
                
                await testEnvironment.StartServer();

                var publishedApplicationMessages = new List<MqttApplicationMessage>();
                testEnvironment.Server.InterceptingPublishAsync += eventArgs =>
                {
                    lock (publishedApplicationMessages)
                    {
                        publishedApplicationMessages.Add(eventArgs.ApplicationMessage);
                    }

                    return CompletedTask.Instance;
                };

                testEnvironment.Server.ValidatingConnectionAsync += eventArgs =>
                {
                    if (eventArgs.UserName == "SECRET")
                    {
                        eventArgs.ReasonCode = MqttConnectReasonCode.Success;
                    }
                    else
                    {
                        eventArgs.ReasonCode = MqttConnectReasonCode.NotAuthorized;
                    }

                    return CompletedTask.Instance;
                };

                using (var validClient = testEnvironment.CreateClient())
                {
                    await validClient.ConnectAsync(
                        testEnvironment.Factory.CreateClientOptionsBuilder()
                            .WithTcpServer("localhost", testEnvironment.ServerPort)
                            .WithCredentials("SECRET")
                            .WithClientId("CLIENT")
                            .Build());

                    await validClient.PublishStringAsync("HELLO 1");

                    // The following code tries to connect a new client with the same client ID but invalid
                    // credentials. This should block the second client but keep the first one up and running.
                    try
                    {
                        using (var invalidClient = testEnvironment.CreateClient())
                        {
                            await invalidClient.ConnectAsync(
                                testEnvironment.Factory.CreateClientOptionsBuilder()
                                    .WithTcpServer("localhost", testEnvironment.ServerPort)
                                    .WithCredentials("???")
                                    .WithClientId("CLIENT")
                                    .Build());
                        }
                    }
                    catch
                    {
                        // Ignore errors from the second client.
                    }

                    await LongTestDelay();
                    
                    await validClient.PublishStringAsync("HELLO 2");
                    
                    await LongTestDelay();
                    
                    await validClient.PublishStringAsync("HELLO 3");
                    
                    await LongTestDelay();
                    
                    Assert.AreEqual(3, publishedApplicationMessages.Count);
                    Assert.AreEqual(1, testEnvironment.Server.GetClientsAsync().GetAwaiter().GetResult().Count);
                }
            }
        }

        [TestMethod]
        public Task Handle_Wrong_Password()
        {
            return TestCredentials("UserName", "x");
        }

        [TestMethod]
        public Task Handle_Wrong_UserName()
        {
            return TestCredentials("x", "Password1");
        }

        [TestMethod]
        public Task Handle_Wrong_UserName_And_Password()
        {
            return TestCredentials("x", "x");
        }

        [TestMethod]
        public async Task Use_Username_Null_Password_Empty()
        {
            string username = null;
            var password = string.Empty;

            using (var testEnvironment = CreateTestEnvironment())
            {
                testEnvironment.IgnoreClientLogErrors = true;

                await testEnvironment.StartServer();

                var client = testEnvironment.CreateClient();

                var clientOptions = new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1", testEnvironment.ServerPort).WithCredentials(username, password).Build();

                var ex = await Assert.ThrowsExceptionAsync<MqttConnectingFailedException>(async () => await client.ConnectAsync(clientOptions));
                Assert.IsInstanceOfType(ex.InnerException, typeof(MqttProtocolViolationException));
                Assert.AreEqual("Error while authenticating. If the User Name Flag is set to 0, the Password Flag MUST be set to 0 [MQTT-3.1.2-22].", ex.Message, false);
                Assert.AreEqual(MqttClientConnectResultCode.UnspecifiedError, ex.ResultCode);
            }
        }

        async Task TestCredentials(string userName, string password)
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                testEnvironment.IgnoreClientLogErrors = true;

                var server = await testEnvironment.StartServer();

                server.ValidatingConnectionAsync += e =>
                {
                    if (e.UserName != "UserName1")
                    {
                        e.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                    }

                    if (e.Password != "Password1")
                    {
                        e.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                    }

                    return CompletedTask.Instance;
                };

                var client = testEnvironment.CreateClient();

                var clientOptions = new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1", testEnvironment.ServerPort).WithCredentials(userName, password).Build();

                var ex = await Assert.ThrowsExceptionAsync<MqttConnectingFailedException>(() => client.ConnectAsync(clientOptions));
                Assert.AreEqual(MqttClientConnectResultCode.BadUserNameOrPassword, ex.Result.ResultCode);
            }
        }
    }
}