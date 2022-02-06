// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Formatter;

namespace MQTTnet.Tests.Server
{
    [TestClass]
    public sealed class Shared_Subscriptions_Tests : BaseTestClass
    {
        [TestMethod]
        public async Task Server_Reports_Shared_Subscriptions_Not_Supported()
        {
            using (var testEnvironment = CreateTestEnvironment(MqttProtocolVersion.V500))
            {
                await testEnvironment.StartServer();

                var client = testEnvironment.CreateClient();
                var connectResult = await client.ConnectAsync(testEnvironment.Factory.CreateClientOptionsBuilder()
                    .WithProtocolVersion(MqttProtocolVersion.V500)
                    .WithTcpServer("127.0.0.1", testEnvironment.ServerPort).Build());

                Assert.IsFalse(connectResult.SharedSubscriptionAvailable);
            }
        }
        
        [TestMethod]
        public async Task Subscription_Of_Shared_Subscription_Is_Denied()
        {
            using (var testEnvironment = CreateTestEnvironment(MqttProtocolVersion.V500))
            {
                await testEnvironment.StartServer();

                var client = testEnvironment.CreateClient();
                await client.ConnectAsync(testEnvironment.Factory.CreateClientOptionsBuilder()
                    .WithProtocolVersion(MqttProtocolVersion.V500)
                    .WithTcpServer("127.0.0.1", testEnvironment.ServerPort).Build());

                var subscribeResult = await client.SubscribeAsync("$share/A");
                
                Assert.AreEqual(MqttClientSubscribeResultCode.SharedSubscriptionsNotSupported, subscribeResult.Items[0].ResultCode);
            }
        }
    }
}