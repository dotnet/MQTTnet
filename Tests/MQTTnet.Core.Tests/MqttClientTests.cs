using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Adapter;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Exceptions;
using MQTTnet.Packets;
using MQTTnet.Server;

namespace MQTTnet.Core.Tests
{
    [TestClass]
    public class MqttClientTests
    {
        [TestMethod]
        public async Task Client_Disconnect_Exception()
        {
            var factory = new MqttFactory();
            var client = factory.CreateMqttClient();

            Exception ex = null;
            client.Disconnected += (s, e) =>
            {
                ex = e.Exception;
            };

            try
            {
                await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("wrong-server").Build());
            }
            catch
            {
            }

            Assert.IsNotNull(ex);
            Assert.IsInstanceOfType(ex, typeof(MqttCommunicationException));
            Assert.IsInstanceOfType(ex.InnerException, typeof(SocketException));
        }

        [TestMethod]
        public async Task Client_Publish()
        {
            var server = new MqttFactory().CreateMqttServer();
            
            try
            {
                var receivedMessages = new List<MqttApplicationMessage>();

                await server.StartAsync(new MqttServerOptions());

                var client1 = new MqttFactory().CreateMqttClient();
                client1.ApplicationMessageReceived += (_, e) =>
                {
                    lock (receivedMessages)
                    {
                        receivedMessages.Add(e.ApplicationMessage);
                    }
                };

                await client1.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1").Build());
                await client1.SubscribeAsync("a");

                var client2 = new MqttFactory().CreateMqttClient();
                await client2.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1").Build());
                var message = new MqttApplicationMessageBuilder().WithTopic("a").WithRetainFlag().Build();
                await client2.PublishAsync(message);

                await Task.Delay(500);

                Assert.AreEqual(1, receivedMessages.Count);
                Assert.IsFalse(receivedMessages.First().Retain); // Must be false even if set above!
            }
            finally
            {
                await server.StopAsync();
            }
        }

        [TestMethod]
        public async Task Publish_Special_Content()
        {
            var factory = new MqttFactory();
            var server = factory.CreateMqttServer();
            var serverOptions = new MqttServerOptionsBuilder().Build();

            var receivedMessages = new List<MqttApplicationMessage>();

            var client = factory.CreateMqttClient();

            try
            {
                await server.StartAsync(serverOptions);

                client.Connected += async (s, e) =>
                {
                    await client.SubscribeAsync("RCU/P1/H0001/R0003");

                    var msg = new MqttApplicationMessageBuilder()
                        .WithPayload("DA|18RS00SC00XI0000RV00R100R200R300R400L100L200L300L400Y100Y200AC0102031800BELK0000BM0000|")
                        .WithTopic("RCU/P1/H0001/R0003");

                    await client.PublishAsync(msg.Build());
                };

                client.ApplicationMessageReceived += (s, e) =>
                {
                    lock (receivedMessages)
                    {
                        receivedMessages.Add(e.ApplicationMessage);
                    }
                };

                await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("localhost").Build());

                await Task.Delay(500);

                Assert.AreEqual(1, receivedMessages.Count);
                Assert.AreEqual("DA|18RS00SC00XI0000RV00R100R200R300R400L100L200L300L400Y100Y200AC0102031800BELK0000BM0000|", receivedMessages.First().ConvertPayloadToString());
            }
            finally
            {
                await server.StopAsync();
            }
        }

#if DEBUG
        [TestMethod]
        public async Task Client_Cleanup_On_Authentification_Fails()
        {
            var channel = new TestMqttCommunicationAdapter();
            var channel2 = new TestMqttCommunicationAdapter();
            channel.Partner = channel2;
            channel2.Partner = channel;

            Task.Run(async () => {
                var connect = await channel2.ReceivePacketAsync(TimeSpan.Zero, CancellationToken.None);
                await channel2.SendPacketAsync(new MqttConnAckPacket
                {
                    ConnectReturnCode = Protocol.MqttConnectReturnCode.ConnectionRefusedNotAuthorized
                }, CancellationToken.None);
            });
            
            var fake = new TestMqttCommunicationAdapterFactory(channel);

            var client = new MqttClient(fake, new MqttNetLogger());

            try
            {
                await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("any-server").Build());
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex, typeof(MqttConnectingFailedException));
            }

            Assert.IsTrue(client._packetReceiverTask == null || client._packetReceiverTask.IsCompleted, "receive loop not completed");
            Assert.IsTrue(client._keepAliveMessageSenderTask == null || client._keepAliveMessageSenderTask.IsCompleted, "keepalive loop not completed");
        }
#endif
    }
}
