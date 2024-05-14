// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Formatter;
using MQTTnet.Internal;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace MQTTnet.Tests.Server
{
    [TestClass]
    public sealed class Unsubscribe_Tests : BaseTestClass
    {
        [TestMethod]
        [ExpectedException(typeof(MqttClientDisconnectedException))]
        public async Task Disconnect_While_Unsubscribing()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServer();

                // The client will be disconnect directly after subscribing!
                server.ClientUnsubscribedTopicAsync += ev => server.DisconnectClientAsync(ev.ClientId, MqttDisconnectReasonCode.NormalDisconnection);

                var client = await testEnvironment.ConnectClient();
                await client.SubscribeAsync("#");
                await client.UnsubscribeAsync("#");
            }
        }

        [TestMethod]
        public async Task Intercept_Unsubscribe_With_User_Properties()
        {
            using (var testEnvironment = CreateTestEnvironment(MqttProtocolVersion.V500))
            {
                var server = await testEnvironment.StartServer();

                InterceptingUnsubscriptionEventArgs eventArgs = null;
                server.InterceptingUnsubscriptionAsync += e =>
                {
                    eventArgs = e;
                    return CompletedTask.Instance;
                };

                var client = await testEnvironment.ConnectClient();

                var unsubscribeOptions = testEnvironment.ClientFactory.CreateUnsubscribeOptionsBuilder().WithTopicFilter("X").WithUserProperty("A", "1").Build();
                await client.UnsubscribeAsync(unsubscribeOptions);

                CollectionAssert.AreEqual(unsubscribeOptions.UserProperties.ToList(), eventArgs.UserProperties);
            }
        }
    }
}