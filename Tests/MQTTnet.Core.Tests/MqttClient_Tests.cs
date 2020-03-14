using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Subscribing;
using MQTTnet.Exceptions;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MQTTnet.Tests.Mockups;

namespace MQTTnet.Tests
{
    [TestClass]
    public class Client_Tests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public async Task Send_Reply_In_Message_Handler_For_Same_Client()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                await testEnvironment.StartServerAsync();
                var client = await testEnvironment.ConnectClientAsync();
                
                await client.SubscribeAsync("#");
                
                var replyReceived = false;

                client.UseApplicationMessageReceivedHandler(c =>
                {
                    if (c.ApplicationMessage.Topic == "request")
                    {
#pragma warning disable 4014
                        Task.Run(() => client.PublishAsync("reply", null, MqttQualityOfServiceLevel.AtLeastOnce));
#pragma warning restore 4014
                    }
                    else
                    {
                        replyReceived = true;
                    }
                });

                await client.PublishAsync("request", null, MqttQualityOfServiceLevel.AtLeastOnce);

                SpinWait.SpinUntil(() => replyReceived, TimeSpan.FromSeconds(10));

                Assert.IsTrue(replyReceived);
            }
        }

        [TestMethod]
        public async Task Send_Reply_In_Message_Handler()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                await testEnvironment.StartServerAsync();
                var client1 = await testEnvironment.ConnectClientAsync();
                var client2 = await testEnvironment.ConnectClientAsync();

                await client1.SubscribeAsync("#");
                await client2.SubscribeAsync("#");

                var replyReceived = false;

                client1.UseApplicationMessageReceivedHandler(c =>
                {
                    if (c.ApplicationMessage.Topic == "reply")
                    {
                        replyReceived = true;
                    }
                });

                client2.UseApplicationMessageReceivedHandler(async c =>{ await client2.PublishAsync("reply", null, MqttQualityOfServiceLevel.AtLeastOnce); });

                await client1.PublishAsync("request", null, MqttQualityOfServiceLevel.AtLeastOnce);

                SpinWait.SpinUntil(() => replyReceived, TimeSpan.FromSeconds(10));

                Assert.IsTrue(replyReceived);
            }
        }

        [TestMethod]
        public async Task Reconnect()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                var server = await testEnvironment.StartServerAsync();
                var client = await testEnvironment.ConnectClientAsync();

                await Task.Delay(500);
                Assert.IsTrue(client.IsConnected);

                await server.StopAsync();
                await Task.Delay(500);
                Assert.IsFalse(client.IsConnected);

                await server.StartAsync(new MqttServerOptionsBuilder().WithDefaultEndpointPort(testEnvironment.ServerPort).Build());
                await Task.Delay(500);

                await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1", testEnvironment.ServerPort).Build());
                Assert.IsTrue(client.IsConnected);
            }
        }

        [TestMethod]
        public async Task Reconnect_While_Server_Offline()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                testEnvironment.IgnoreClientLogErrors = true;

                var server = await testEnvironment.StartServerAsync();
                var client = await testEnvironment.ConnectClientAsync();

                await Task.Delay(500);
                Assert.IsTrue(client.IsConnected);

                await server.StopAsync();
                await Task.Delay(500);
                Assert.IsFalse(client.IsConnected);

                for (var i = 0; i < 5; i++)
                {
                    try
                    {
                        await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1", testEnvironment.ServerPort).Build());
                        Assert.Fail("Must fail!");
                    }
                    catch
                    {
                    }
                }

                await server.StartAsync(new MqttServerOptionsBuilder().WithDefaultEndpointPort(testEnvironment.ServerPort).Build());
                await Task.Delay(500);

                await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1", testEnvironment.ServerPort).Build());
                Assert.IsTrue(client.IsConnected);
            }
        }

        [TestMethod]
        public async Task Reconnect_From_Disconnected_Event()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                testEnvironment.IgnoreClientLogErrors = true;

                var client = testEnvironment.CreateClient();

                var tries = 0;
                var maxTries = 3;

                client.UseDisconnectedHandler(async e =>
                {
                    if (tries >= maxTries)
                    {
                        return;
                    }

                    Interlocked.Increment(ref tries);

                    await Task.Delay(100);
                    await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1", testEnvironment.ServerPort).Build());
                });

                try
                {
                    await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1", testEnvironment.ServerPort).Build());
                    Assert.Fail("Must fail!");
                }
                catch
                {
                }
                
                SpinWait.SpinUntil(() => tries >= maxTries, 10000);

                Assert.AreEqual(maxTries, tries);
            }
        }

        [TestMethod]
        public async Task PacketIdentifier_In_Publish_Result()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                await testEnvironment.StartServerAsync();
                var client = await testEnvironment.ConnectClientAsync();

                var result = await client.PublishAsync("a", "a", MqttQualityOfServiceLevel.AtMostOnce);
                Assert.AreEqual(null, result.PacketIdentifier);

                result = await client.PublishAsync("b", "b", MqttQualityOfServiceLevel.AtMostOnce);
                Assert.AreEqual(null, result.PacketIdentifier);

                result = await client.PublishAsync("a", "a", MqttQualityOfServiceLevel.AtLeastOnce);
                Assert.AreEqual((ushort)1, result.PacketIdentifier);

                result = await client.PublishAsync("b", "b", MqttQualityOfServiceLevel.AtLeastOnce);
                Assert.AreEqual((ushort)2, result.PacketIdentifier);

                result = await client.PublishAsync("a", "a", MqttQualityOfServiceLevel.ExactlyOnce);
                Assert.AreEqual((ushort)3, result.PacketIdentifier);

                result = await client.PublishAsync("b", "b", MqttQualityOfServiceLevel.ExactlyOnce);
                Assert.AreEqual((ushort)4, result.PacketIdentifier);
            }
        }
        
        [TestMethod]
        public async Task Invalid_Connect_Throws_Exception()
        {
            var factory = new MqttFactory();
            using (var client = factory.CreateMqttClient())
            {
                try
                {
                    await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("wrong-server").Build());

                    Assert.Fail("Must fail!");
                }
                catch (Exception exception)
                {
                    Assert.IsNotNull(exception);
                    Assert.IsInstanceOfType(exception, typeof(MqttCommunicationException));
                    Assert.IsInstanceOfType(exception.InnerException, typeof(SocketException));
                }
            }
        }

        [TestMethod]
        public async Task ConnectTimeout_Throws_Exception()
        {
            var factory = new MqttFactory();
            using (var client = factory.CreateMqttClient())
            {
                bool disconnectHandlerCalled = false;
                try
                {
                    client.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(args =>
                    {
                        disconnectHandlerCalled = true;
                    });

                    await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("1.2.3.4").Build());

                    Assert.Fail("Must fail!");
                }
                catch (Exception exception)
                {
                    Assert.IsNotNull(exception);
                    Assert.IsInstanceOfType(exception, typeof(MqttCommunicationException));
                    //Assert.IsInstanceOfType(exception.InnerException, typeof(SocketException));
                }

                await Task.Delay(100); // disconnected handler is called async
                Assert.IsTrue(disconnectHandlerCalled);
            }
        }

        [TestMethod]
        public async Task Fire_Disconnected_Event_On_Server_Shutdown()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                var server = await testEnvironment.StartServerAsync();
                var client = await testEnvironment.ConnectClientAsync();

                var handlerFired = false;
                client.UseDisconnectedHandler(e => handlerFired = true);

                await server.StopAsync();

                await Task.Delay(4000);

                Assert.IsTrue(handlerFired);
            }
        }

        [TestMethod]
        public async Task Disconnect_Event_Contains_Exception()
        {
            var factory = new MqttFactory();
            using (var client = factory.CreateMqttClient())
            {
                Exception ex = null;
                client.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(e =>
                {
                    ex = e.Exception;
                });

                try
                {
                    await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("wrong-server").Build());
                }
                catch
                {
                }

                await Task.Delay(500);

                Assert.IsNotNull(ex);
                Assert.IsInstanceOfType(ex, typeof(MqttCommunicationException));
                Assert.IsInstanceOfType(ex.InnerException, typeof(SocketException));
            }
        }

        [TestMethod]
        public async Task Preserve_Message_Order()
        {
            // The messages are sent in reverse or to ensure that the delay in the handler
            // needs longer for the first messages and later messages may be processed earlier (if there
            // is an issue).
            const int MessagesCount = 50;

            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                await testEnvironment.StartServerAsync();

                var client1 = await testEnvironment.ConnectClientAsync();
                await client1.SubscribeAsync("x");

                var receivedValues = new List<int>();

                async Task Handler1(MqttApplicationMessageReceivedEventArgs eventArgs)
                {
                    var value = int.Parse(eventArgs.ApplicationMessage.ConvertPayloadToString());
                    await Task.Delay(value);

                    lock (receivedValues)
                    {
                        receivedValues.Add(value);
                    }
                }

                client1.UseApplicationMessageReceivedHandler(Handler1);

                var client2 = await testEnvironment.ConnectClientAsync();
                for (var i = MessagesCount; i > 0; i--)
                {
                    await client2.PublishAsync("x", i.ToString());
                }

                await Task.Delay(5000);

                for (var i = MessagesCount; i > 0; i--)
                {
                    Assert.AreEqual(i, receivedValues[MessagesCount - i]);
                }
            }
        }

        [TestMethod]
        public async Task Send_Reply_For_Any_Received_Message()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                await testEnvironment.StartServerAsync();

                var client1 = await testEnvironment.ConnectClientAsync();
                await client1.SubscribeAsync("request/+");

                async Task Handler1(MqttApplicationMessageReceivedEventArgs eventArgs)
                {
                    await client1.PublishAsync($"reply/{eventArgs.ApplicationMessage.Topic}");
                }

                client1.UseApplicationMessageReceivedHandler(Handler1);

                var client2 = await testEnvironment.ConnectClientAsync();
                await client2.SubscribeAsync("reply/#");

                var replies = new List<string>();

                void Handler2(MqttApplicationMessageReceivedEventArgs eventArgs)
                {
                    lock (replies)
                    {
                        replies.Add(eventArgs.ApplicationMessage.Topic);
                    }
                }

                client2.UseApplicationMessageReceivedHandler((Action<MqttApplicationMessageReceivedEventArgs>)Handler2);

                await Task.Delay(500);

                await client2.PublishAsync("request/a");
                await client2.PublishAsync("request/b");
                await client2.PublishAsync("request/c");

                await Task.Delay(500);

                Assert.AreEqual("reply/request/a,reply/request/b,reply/request/c", string.Join(",", replies));
            }
        }

        [TestMethod]
        public async Task Publish_With_Correct_Retain_Flag()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                await testEnvironment.StartServerAsync();

                var receivedMessages = new List<MqttApplicationMessage>();

                var client1 = await testEnvironment.ConnectClientAsync();
                client1.UseApplicationMessageReceivedHandler(c =>
                {
                    lock (receivedMessages)
                    {
                        receivedMessages.Add(c.ApplicationMessage);
                    }
                });

                await client1.SubscribeAsync("a");

                var client2 = await testEnvironment.ConnectClientAsync();
                var message = new MqttApplicationMessageBuilder().WithTopic("a").WithRetainFlag().Build();
                await client2.PublishAsync(message);

                await Task.Delay(500);

                Assert.AreEqual(1, receivedMessages.Count);
                Assert.IsFalse(receivedMessages.First().Retain); // Must be false even if set above!
            }
        }

        [TestMethod]
        public async Task Subscribe_In_Callback_Events()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                await testEnvironment.StartServerAsync();

                var receivedMessages = new List<MqttApplicationMessage>();

                var client = testEnvironment.CreateClient();

                client.ConnectedHandler = new MqttClientConnectedHandlerDelegate(async e =>
                {
                    await client.SubscribeAsync("RCU/P1/H0001/R0003");

                    var msg = new MqttApplicationMessageBuilder()
                        .WithPayload("DA|18RS00SC00XI0000RV00R100R200R300R400L100L200L300L400Y100Y200AC0102031800BELK0000BM0000|")
                        .WithTopic("RCU/P1/H0001/R0003");

                    await client.PublishAsync(msg.Build());
                });

                client.UseApplicationMessageReceivedHandler(c =>
                {
                    lock (receivedMessages)
                    {
                        receivedMessages.Add(c.ApplicationMessage);
                    }
                });

                await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("localhost", testEnvironment.ServerPort).Build());

                await Task.Delay(500);

                Assert.AreEqual(1, receivedMessages.Count);
                Assert.AreEqual("DA|18RS00SC00XI0000RV00R100R200R300R400L100L200L300L400Y100Y200AC0102031800BELK0000BM0000|", receivedMessages.First().ConvertPayloadToString());
            }
        }

        [TestMethod]
        public async Task Message_Send_Retry()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                testEnvironment.IgnoreClientLogErrors = true;
                testEnvironment.IgnoreServerLogErrors = true;

                await testEnvironment.StartServerAsync(
                    new MqttServerOptionsBuilder()
                        .WithPersistentSessions()
                        .WithDefaultCommunicationTimeout(TimeSpan.FromMilliseconds(250)));

                var client1 = await testEnvironment.ConnectClientAsync(new MqttClientOptionsBuilder().WithCleanSession(false));
                await client1.SubscribeAsync("x", MqttQualityOfServiceLevel.AtLeastOnce);

                var retries = 0;

                async Task Handler1(MqttApplicationMessageReceivedEventArgs eventArgs)
                {
                    retries++;

                    await Task.Delay(1000);
                    throw new Exception("Broken!");
                }

                client1.UseApplicationMessageReceivedHandler(Handler1);

                var client2 = await testEnvironment.ConnectClientAsync();
                await client2.PublishAsync("x");

                await Task.Delay(3000);

                // The server should disconnect clients which are not responding.
                Assert.IsFalse(client1.IsConnected);

                await client1.ReconnectAsync().ConfigureAwait(false);

                await Task.Delay(1000);

                Assert.AreEqual(2, retries);
            }
        }

        [TestMethod]
        public async Task NoConnectedHandler_Connect_DoesNotThrowException()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                await testEnvironment.StartServerAsync();

                var client = await testEnvironment.ConnectClientAsync();

                Assert.IsTrue(client.IsConnected);
            }
        }

        [TestMethod]
        public async Task NoDisconnectedHandler_Disconnect_DoesNotThrowException()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                await testEnvironment.StartServerAsync();
                var client = await testEnvironment.ConnectClientAsync();
                Assert.IsTrue(client.IsConnected);

                await client.DisconnectAsync();

                Assert.IsFalse(client.IsConnected);
            }
        }

        [TestMethod]
        public async Task Frequent_Connects()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                await testEnvironment.StartServerAsync();

                var clients = new List<IMqttClient>();
                for (var i = 0; i < 100; i++)
                {
                    clients.Add(await testEnvironment.ConnectClientAsync(new MqttClientOptionsBuilder().WithClientId("a")));
                }

                var clientStatus = await testEnvironment.Server.GetClientStatusAsync();
                var sessionStatus = await testEnvironment.Server.GetSessionStatusAsync();

                for (var i = 0; i < 98; i++)
                {
                    Assert.IsFalse(clients[i].IsConnected);
                }
                
                Assert.IsTrue(clients[99].IsConnected);

                Assert.AreEqual(1, clientStatus.Count);
                Assert.AreEqual(1, sessionStatus.Count);

                var receiveClient = clients[99];
                object receivedPayload = null;
                receiveClient.UseApplicationMessageReceivedHandler(e =>
                {
                    receivedPayload = e.ApplicationMessage.ConvertPayloadToString();
                });

                await receiveClient.SubscribeAsync("x");

                var sendClient = await testEnvironment.ConnectClientAsync();
                await sendClient.PublishAsync("x", "1");

                await Task.Delay(100);

                Assert.AreEqual("1", receivedPayload);
            }
        }

        [TestMethod]
        public async Task No_Payload()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                await testEnvironment.StartServerAsync();

                var sender = await testEnvironment.ConnectClientAsync();
                var receiver = await testEnvironment.ConnectClientAsync();

                var message = new MqttApplicationMessageBuilder()
                    .WithTopic("A");

                await receiver.SubscribeAsync(new MqttClientSubscribeOptions
                {
                    TopicFilters = new List<TopicFilter> { new TopicFilter { Topic = "#" } }
                }, CancellationToken.None);

                MqttApplicationMessage receivedMessage = null;
                receiver.UseApplicationMessageReceivedHandler(e => receivedMessage = e.ApplicationMessage);

                await sender.PublishAsync(message.Build(), CancellationToken.None);

                await Task.Delay(1000);

                Assert.IsNotNull(receivedMessage);
                Assert.AreEqual("A", receivedMessage.Topic);
                Assert.AreEqual(null, receivedMessage.Payload);
            }
        }
    }
}
