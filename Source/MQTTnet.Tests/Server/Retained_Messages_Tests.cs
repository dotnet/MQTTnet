// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Tests.Server
{
    [TestClass]
    public sealed class Retained_Messages_Tests : BaseTestClass
    {
        [TestMethod]
        public async Task Clear_Retained_Message_With_Empty_Payload()
        {
            using var testEnvironments = CreateMixedTestEnvironment();
            foreach (var testEnvironment in testEnvironments)
            {
                await testEnvironment.StartServer();

                var c1 = await testEnvironment.ConnectClient();

                await c1.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("retained").WithPayload(new byte[3]).WithRetainFlag().Build());
                await c1.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("retained").WithPayload(new byte[0]).WithRetainFlag().Build());

                await c1.DisconnectAsync();

                var c2 = await testEnvironment.ConnectClient();
                var messageHandler = testEnvironment.CreateApplicationMessageHandler(c2);

                await Task.Delay(200);
                await c2.SubscribeAsync(new MqttTopicFilter { Topic = "retained", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce });
                await Task.Delay(500);

                messageHandler.AssertReceivedCountEquals(0);
            }
        }

        [TestMethod]
        public async Task Clear_Retained_Message_With_Null_Payload()
        {
            using var testEnvironments = CreateMixedTestEnvironment();
            foreach (var testEnvironment in testEnvironments)
            {
                await testEnvironment.StartServer();

                var c1 = await testEnvironment.ConnectClient();

                await c1.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("retained").WithPayload(new byte[3]).WithRetainFlag().Build());
                await c1.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("retained").WithPayload((byte[])null).WithRetainFlag().Build());

                await c1.DisconnectAsync();

                var c2 = await testEnvironment.ConnectClient();
                var messageHandler = testEnvironment.CreateApplicationMessageHandler(c2);

                await Task.Delay(200);
                await c2.SubscribeAsync(new MqttTopicFilter { Topic = "retained", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce });
                await Task.Delay(500);

                messageHandler.AssertReceivedCountEquals(0);
            }
        }

        [TestMethod]
        public async Task Downgrade_QoS_Level()
        {
            using var testEnvironments = CreateMixedTestEnvironment();
            foreach (var testEnvironment in testEnvironments)
            {
                await testEnvironment.StartServer();

                var c1 = await testEnvironment.ConnectClient();

                // Add the retained message with QoS 2!
                await c1.PublishAsync(
                    new MqttApplicationMessageBuilder().WithTopic("retained")
                        .WithPayload(new byte[3])
                        .WithRetainFlag()
                        .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
                        .Build());

                // The second client uses QoS 1 so a downgrade is required.
                var c2 = await testEnvironment.ConnectClient();
                var messageHandler = testEnvironment.CreateApplicationMessageHandler(c2);
                await c2.SubscribeAsync(new MqttTopicFilter { Topic = "retained", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce });

                await LongTestDelay();

                messageHandler.AssertReceivedCountEquals(1);

                Assert.AreEqual(MqttQualityOfServiceLevel.AtLeastOnce, messageHandler.ReceivedEventArgs.First().ApplicationMessage.QualityOfServiceLevel);
            }
        }

        [TestMethod]
        public async Task No_Upgrade_QoS_Level()
        {
            using var testEnvironments = CreateMixedTestEnvironment();
            foreach (var testEnvironment in testEnvironments)
            {
                await testEnvironment.StartServer();

                var c1 = await testEnvironment.ConnectClient();

                // Add the retained message with QoS 1!
                await c1.PublishAsync(
                    new MqttApplicationMessageBuilder().WithTopic("retained")
                        .WithPayload(new byte[3])
                        .WithRetainFlag()
                        .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                        .Build());

                // The second client uses QoS 2 so an upgrade is expected but according to the MQTT spec this is not supported!
                var c2 = await testEnvironment.ConnectClient();
                var messageHandler = testEnvironment.CreateApplicationMessageHandler(c2);
                await c2.SubscribeAsync(new MqttTopicFilter { Topic = "retained", QualityOfServiceLevel = MqttQualityOfServiceLevel.ExactlyOnce });

                await LongTestDelay();

                messageHandler.AssertReceivedCountEquals(1);

                Assert.AreEqual(MqttQualityOfServiceLevel.AtLeastOnce, messageHandler.ReceivedEventArgs.First().ApplicationMessage.QualityOfServiceLevel);
            }
        }

        [TestMethod]
        public async Task Receive_No_Retained_Message_After_Subscribe()
        {
            using var testEnvironments = CreateMixedTestEnvironment();
            foreach (var testEnvironment in testEnvironments)
            {
                await testEnvironment.StartServer();

                var c1 = await testEnvironment.ConnectClient();
                await c1.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("retained").WithPayload(new byte[3]).WithRetainFlag().Build());
                await c1.DisconnectAsync();

                var c2 = await testEnvironment.ConnectClient();
                var messageHandler = testEnvironment.CreateApplicationMessageHandler(c2);
                await c2.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("retained_other").Build());

                await Task.Delay(500);

                messageHandler.AssertReceivedCountEquals(0);
            }
        }

        [TestMethod]
        public async Task Receive_Retained_Message_After_Subscribe()
        {
            using var testEnvironments = CreateMixedTestEnvironment();
            foreach (var testEnvironment in testEnvironments)
            {
                await testEnvironment.StartServer();

                var c1 = await testEnvironment.ConnectClient();
                await c1.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("retained").WithPayload(new byte[3]).WithRetainFlag().Build());
                await c1.DisconnectAsync();

                var c2 = await testEnvironment.ConnectClient();
                var messageHandler = testEnvironment.CreateApplicationMessageHandler(c2);

                await c2.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("retained").Build());

                await Task.Delay(500);

                messageHandler.AssertReceivedCountEquals(1);
                Assert.IsTrue(messageHandler.ReceivedEventArgs.First().ApplicationMessage.Retain);
            }
        }

        [TestMethod]
        public async Task Receive_Retained_Messages_From_Higher_Qos_Level()
        {
            using var testEnvironments = CreateMixedTestEnvironment();
            foreach (var testEnvironment in testEnvironments)
            {
                await testEnvironment.StartServer();

                // Upload retained message.
                var c1 = await testEnvironment.ConnectClient();
                await c1.PublishAsync(
                    new MqttApplicationMessageBuilder().WithTopic("retained")
                        .WithPayload(new byte[1])
                        .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                        .WithRetainFlag()
                        .Build());

                await c1.DisconnectAsync();

                // Subscribe using a new client.
                var c2 = await testEnvironment.ConnectClient();
                var messageHandler = testEnvironment.CreateApplicationMessageHandler(c2);

                await Task.Delay(200);
                // Using QoS 2 will lead to 1 instead because the publish was made with QoS level 1 (see 3.8.4 SUBSCRIBE Actions)!
                await c2.SubscribeAsync(new MqttTopicFilter { Topic = "retained", QualityOfServiceLevel = MqttQualityOfServiceLevel.ExactlyOnce });
                await Task.Delay(500);

                messageHandler.AssertReceivedCountEquals(1);
            }
        }

        [TestMethod]
        public async Task Retained_Messages_Flow()
        {
            using var testEnvironments = CreateMixedTestEnvironment();
            foreach (var testEnvironment in testEnvironments)
            {
                var retainedMessage = new MqttApplicationMessageBuilder().WithTopic("r").WithPayload("r").WithRetainFlag().Build();

                await testEnvironment.StartServer();
                var c1 = await testEnvironment.ConnectClient();

                var c2 = await testEnvironment.ConnectClient();
                var messageHandler = testEnvironment.CreateApplicationMessageHandler(c2);

                await c1.PublishAsync(retainedMessage);
                await c1.DisconnectAsync();
                await LongTestDelay();

                for (var i = 0; i < 5; i++)
                {
                    await c2.UnsubscribeAsync("r");
                    await Task.Delay(100);
                    messageHandler.AssertReceivedCountEquals(i);

                    await c2.SubscribeAsync("r");
                    await Task.Delay(100);
                    messageHandler.AssertReceivedCountEquals(i + 1);
                }

                await c2.DisconnectAsync();
            }
        }

        [TestMethod]
        public async Task Server_Reports_Retained_Messages_Supported_V3()
        {
            using var testEnvironments = CreateMixedTestEnvironment();
            foreach (var testEnvironment in testEnvironments)
            {
                await testEnvironment.StartServer();

                var client = testEnvironment.CreateClient();

                var connectResult = await client.ConnectAsync(
                    testEnvironment.ClientFactory.CreateClientOptionsBuilder()
                        .WithProtocolVersion(MqttProtocolVersion.V311)
                        .WithTcpServer("127.0.0.1", testEnvironment.ServerPort)
                        .Build());

                Assert.IsTrue(connectResult.RetainAvailable);
            }
        }

        [TestMethod]
        public async Task Server_Reports_Retained_Messages_Supported_V5()
        {
            using var testEnvironments = CreateMixedTestEnvironment(MqttProtocolVersion.V500);
            foreach (var testEnvironment in testEnvironments)
            {
                await testEnvironment.StartServer();

                var client = testEnvironment.CreateClient();
                var connectResult = await client.ConnectAsync(
                    testEnvironment.ClientFactory.CreateClientOptionsBuilder()
                        .WithProtocolVersion(MqttProtocolVersion.V500)
                        .WithTcpServer("127.0.0.1", testEnvironment.ServerPort)
                        .Build());

                Assert.IsTrue(connectResult.RetainAvailable);
            }
        }
    }
}