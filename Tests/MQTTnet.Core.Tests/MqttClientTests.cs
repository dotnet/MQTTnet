using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using MQTTnet.Exceptions;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MQTTnet.Tests.Mockups;

namespace MQTTnet.Tests
{
    [TestClass]
    public class Client_Tests
    {
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
        public async Task Disconnect_Event_Contains_Exception()
        {
            var factory = new MqttFactory();
            using (var client = factory.CreateMqttClient())
            {
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
        }

        [TestMethod]
        public async Task Preserve_Message_Order()
        {
            // The messages are sent in reverse or to ensure that the delay in the handler
            // needs longer for the first messages and later messages may be processed earlier (if there
            // is an issue).
            const int MessagesCount = 50;

            using (var testEnvironment = new TestEnvironment())
            {
                await testEnvironment.StartServerAsync();

                var client1 = await testEnvironment.ConnectClientAsync();
                await client1.SubscribeAsync("x");

                var receivedValues = new List<int>();
     
                async Task Handler1(MqttApplicationMessageHandlerContext context)
                {
                    var value = int.Parse(context.ApplicationMessage.ConvertPayloadToString());
                    await Task.Delay(value);

                    lock (receivedValues)
                    {
                        receivedValues.Add(value);
                    }
                }

                client1.UseReceivedApplicationMessageHandler(Handler1);

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
            using (var testEnvironment = new TestEnvironment())
            {
                await testEnvironment.StartServerAsync();

                var client1 = await testEnvironment.ConnectClientAsync();
                await client1.SubscribeAsync("request/+");

                async Task Handler1(MqttApplicationMessageHandlerContext context)
                {
                    await client1.PublishAsync($"reply/{context.ApplicationMessage.Topic}");
                }

                client1.UseReceivedApplicationMessageHandler(Handler1);

                var client2 = await testEnvironment.ConnectClientAsync();
                await client2.SubscribeAsync("reply/#");

                var replies = new List<string>();

                void Handler2(MqttApplicationMessageHandlerContext context)
                {
                    lock (replies)
                    {
                        replies.Add(context.ApplicationMessage.Topic);
                    }
                }
                
                client2.UseReceivedApplicationMessageHandler((Action<MqttApplicationMessageHandlerContext>) Handler2);

                await Task.Delay(500);

                await client2.PublishAsync("request/a");
                await client2.PublishAsync("request/b");
                await client2.PublishAsync("request/c");

                await Task.Delay(500);

                Assert.AreEqual("reply/request/a,reply/request/b,reply/request/c", string.Join("," , replies));
            }
        }

        [TestMethod]
        public async Task Publish_With_Correct_Retain_Flag()
        {
            using (var testEnvironment = new TestEnvironment())
            {
                await testEnvironment.StartServerAsync();

                var receivedMessages = new List<MqttApplicationMessage>();

                var client1 = await testEnvironment.ConnectClientAsync();
                client1.UseReceivedApplicationMessageHandler(c =>
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
            using (var testEnvironment = new TestEnvironment())
            {
                await testEnvironment.StartServerAsync();

                var receivedMessages = new List<MqttApplicationMessage>();

                var client = testEnvironment.CreateClient();

                client.Connected += async (s, e) =>
                {
                    await client.SubscribeAsync("RCU/P1/H0001/R0003");

                    var msg = new MqttApplicationMessageBuilder()
                        .WithPayload("DA|18RS00SC00XI0000RV00R100R200R300R400L100L200L300L400Y100Y200AC0102031800BELK0000BM0000|")
                        .WithTopic("RCU/P1/H0001/R0003");

                    await client.PublishAsync(msg.Build());
                };

                client.UseReceivedApplicationMessageHandler(c =>
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
            using (var testEnvironment = new TestEnvironment())
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

                async Task Handler1(MqttApplicationMessageHandlerContext context)
                {
                    retries++;

                    await Task.Delay(1000);
                    throw new Exception("Broken!");
                }

                client1.UseReceivedApplicationMessageHandler(Handler1);

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
    }
}
