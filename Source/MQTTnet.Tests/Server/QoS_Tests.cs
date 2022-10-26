// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Internal;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace MQTTnet.Tests.Server
{
    [TestClass]
    public sealed class QoS_Tests : BaseTestClass
    {
        [TestMethod]
        public async Task Fire_Event_On_Client_Acknowledges_QoS_0()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServer();

                ClientAcknowledgedPublishPacketEventArgs eventArgs = null;
                server.ClientAcknowledgedPublishPacketAsync += args =>
                {
                    eventArgs = args;
                    return CompletedTask.Instance;
                };

                var client1 = await testEnvironment.ConnectClient();
                await client1.SubscribeAsync("A");

                var client2 = await testEnvironment.ConnectClient();
                await client2.PublishStringAsync("A");

                await LongTestDelay();

                // Must be null because no event should be fired for QoS 0.
                Assert.IsNull(eventArgs);
            }
        }

        [TestMethod]
        public async Task Fire_Event_On_Client_Acknowledges_QoS_1()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServer();

                ClientAcknowledgedPublishPacketEventArgs eventArgs = null;
                server.ClientAcknowledgedPublishPacketAsync += args =>
                {
                    eventArgs = args;
                    return CompletedTask.Instance;
                };

                var client1 = await testEnvironment.ConnectClient();
                await client1.SubscribeAsync("A", MqttQualityOfServiceLevel.AtLeastOnce);

                var client2 = await testEnvironment.ConnectClient();
                await client2.PublishStringAsync("A", qualityOfServiceLevel: MqttQualityOfServiceLevel.AtLeastOnce);

                await LongTestDelay();

                Assert.IsNotNull(eventArgs);
                Assert.IsNotNull(eventArgs.PublishPacket);
                Assert.IsNotNull(eventArgs.AcknowledgePacket);
                Assert.IsTrue(eventArgs.IsCompleted);

                Assert.AreEqual("A", eventArgs.PublishPacket.Topic);
            }
        }

        [TestMethod]
        public async Task Fire_Event_On_Client_Acknowledges_QoS_2()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServer();

                var eventArgs = new List<ClientAcknowledgedPublishPacketEventArgs>();
                server.ClientAcknowledgedPublishPacketAsync += args =>
                {
                    lock (eventArgs)
                    {
                        eventArgs.Add(args);
                    }

                    return CompletedTask.Instance;
                };

                var client1 = await testEnvironment.ConnectClient();
                await client1.SubscribeAsync("A", MqttQualityOfServiceLevel.ExactlyOnce);

                var client2 = await testEnvironment.ConnectClient();
                await client2.PublishStringAsync("A", qualityOfServiceLevel: MqttQualityOfServiceLevel.ExactlyOnce);

                await LongTestDelay();

                Assert.AreEqual(1, eventArgs.Count);

                var firstEvent = eventArgs[0];

                Assert.IsNotNull(firstEvent);
                Assert.IsNotNull(firstEvent.PublishPacket);
                Assert.IsNotNull(firstEvent.AcknowledgePacket);
                Assert.IsFalse(firstEvent.IsCompleted);

                Assert.AreEqual("A", firstEvent.PublishPacket.Topic);

                var secondEvent = eventArgs[1];

                Assert.IsNotNull(secondEvent);
                Assert.IsNotNull(secondEvent.PublishPacket);
                Assert.IsNotNull(secondEvent.AcknowledgePacket);
                Assert.IsTrue(secondEvent.IsCompleted);

                Assert.AreEqual("A", secondEvent.PublishPacket.Topic);
            }
        }
    }
}