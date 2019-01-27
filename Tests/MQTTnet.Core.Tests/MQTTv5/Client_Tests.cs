using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Client.Publishing;
using MQTTnet.Client.Subscribing;
using MQTTnet.Client.Unsubscribing;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace MQTTnet.Tests.MQTTv5
{
    [TestClass]
    public class Client_Tests
    {
        [TestMethod]
        public async Task Connect()
        {
            var server = new MqttFactory().CreateMqttServer();
            var client = new MqttFactory().CreateMqttClient();

            try
            {
                await server.StartAsync(new MqttServerOptions());
                await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1").WithProtocolVersion(MqttProtocolVersion.V500).Build());
            }
            finally
            {
                await server.StopAsync();
            }
        }

        [TestMethod]
        public async Task Connect_And_Disconnect()
        {
            var server = new MqttFactory().CreateMqttServer();
            var client = new MqttFactory().CreateMqttClient();

            try
            {
                await server.StartAsync(new MqttServerOptions());

                await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1").WithProtocolVersion(MqttProtocolVersion.V500).Build());
                await client.DisconnectAsync();
            }
            finally
            {
                await server.StopAsync();
            }
        }

        [TestMethod]
        public async Task Subscribe()
        {
            var server = new MqttFactory().CreateMqttServer();
            var client = new MqttFactory().CreateMqttClient();

            try
            {
                await server.StartAsync(new MqttServerOptions());

                await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1").WithProtocolVersion(MqttProtocolVersion.V500).Build());
                var result = await client.SubscribeAsync("a", MqttQualityOfServiceLevel.AtLeastOnce);
                await client.DisconnectAsync();

                Assert.AreEqual(1, result.Items.Count);
                Assert.AreEqual(MqttClientSubscribeResultCode.GrantedQoS1, result.Items[0].ResultCode);
            }
            finally
            {
                await server.StopAsync();
            }
        }

        [TestMethod]
        public async Task Unsubscribe()
        {
            var server = new MqttFactory().CreateMqttServer();
            var client = new MqttFactory().CreateMqttClient();

            try
            {
                await server.StartAsync(new MqttServerOptions());

                await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1").WithProtocolVersion(MqttProtocolVersion.V500).Build());
                await client.SubscribeAsync("a");
                var result = await client.UnsubscribeAsync("a");
                await client.DisconnectAsync();

                Assert.AreEqual(1, result.Items.Count);
                Assert.AreEqual(MqttClientUnsubscribeResultCode.Success, result.Items[0].ReasonCode);
            }
            finally
            {
                await server.StopAsync();
            }
        }

        [TestMethod]
        public async Task Publish_QoS_0()
        {
            var server = new MqttFactory().CreateMqttServer();
            var client = new MqttFactory().CreateMqttClient();

            try
            {
                await server.StartAsync(new MqttServerOptions());

                await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1").WithProtocolVersion(MqttProtocolVersion.V500).Build());
                var result = await client.PublishAsync("a", "b");
                await client.DisconnectAsync();

                Assert.AreEqual(MqttClientPublishReasonCode.Success, result.ReasonCode);
            }
            finally
            {
                await server.StopAsync();
            }
        }

        [TestMethod]
        public async Task Publish_QoS_1()
        {
            var server = new MqttFactory().CreateMqttServer();
            var client = new MqttFactory().CreateMqttClient();

            try
            {
                await server.StartAsync(new MqttServerOptions());

                await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1").WithProtocolVersion(MqttProtocolVersion.V500).Build());
                var result = await client.PublishAsync("a", "b", MqttQualityOfServiceLevel.AtLeastOnce);
                await client.DisconnectAsync();

                Assert.AreEqual(MqttClientPublishReasonCode.Success, result.ReasonCode);
            }
            finally
            {
                await server.StopAsync();
            }
        }

        [TestMethod]
        public async Task Publish_QoS_2()
        {
            var server = new MqttFactory().CreateMqttServer();
            var client = new MqttFactory().CreateMqttClient();

            try
            {
                await server.StartAsync(new MqttServerOptions());

                await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1").WithProtocolVersion(MqttProtocolVersion.V500).Build());
                var result = await client.PublishAsync("a", "b", MqttQualityOfServiceLevel.ExactlyOnce);
                await client.DisconnectAsync();

                Assert.AreEqual(MqttClientPublishReasonCode.Success, result.ReasonCode);
            }
            finally
            {
                await server.StopAsync();
            }
        }

        [TestMethod]
        public async Task Publish_With_Properties()
        {
            var server = new MqttFactory().CreateMqttServer();
            var client = new MqttFactory().CreateMqttClient();

            try
            {
                await server.StartAsync(new MqttServerOptions());

                var applicationMessage = new MqttApplicationMessageBuilder()
                    .WithTopic("Hello")
                    .WithPayload("World")
                    .WithAtMostOnceQoS()
                    .WithUserProperty("x", "1")
                    .WithUserProperty("y", "2")
                    .WithResponseTopic("response")
                    .WithContentType("text")
                    .WithMessageExpiryInterval(50)
                    .WithCorrelationData(new byte[12])
                    .WithTopicAlias(2)
                    .Build();

                await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1").WithProtocolVersion(MqttProtocolVersion.V500).Build());
                var result = await client.PublishAsync(applicationMessage);
                await client.DisconnectAsync();

                Assert.AreEqual(MqttClientPublishReasonCode.Success, result.ReasonCode);
            }
            finally
            {
                await server.StopAsync();
            }
        }

        [TestMethod]
        public async Task Subscribe_And_Publish()
        {
            var server = new MqttFactory().CreateMqttServer();
            var client1 = new MqttFactory().CreateMqttClient();
            var client2 = new MqttFactory().CreateMqttClient();

            try
            {
                await server.StartAsync(new MqttServerOptions());

                var receivedMessages = new List<MqttApplicationMessageReceivedEventArgs>();

                await client1.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1").WithClientId("client1").WithProtocolVersion(MqttProtocolVersion.V500).Build());
                client1.ApplicationMessageReceived += (s, e) =>
                {
                    lock (receivedMessages)
                    {
                        receivedMessages.Add(e);
                    }
                };

                await client1.SubscribeAsync("a");

                await client2.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1").WithClientId("client2").WithProtocolVersion(MqttProtocolVersion.V500).Build());
                await client2.PublishAsync("a", "b");

                await Task.Delay(500);

                await client2.DisconnectAsync();
                await client1.DisconnectAsync();

                Assert.AreEqual(1, receivedMessages.Count);
                Assert.AreEqual("client1", receivedMessages[0].ClientId);
                Assert.AreEqual("a", receivedMessages[0].ApplicationMessage.Topic);
                Assert.AreEqual("b", receivedMessages[0].ApplicationMessage.ConvertPayloadToString());
            }
            finally
            {
                await server.StopAsync();
            }
        }
    }
}
