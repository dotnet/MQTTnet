using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MQTTnet.Tests.Mockups;

namespace MQTTnet.Tests.Server
{
    [TestClass]
    public sealed class Session_Tests : BaseTestClass
    { 
        [TestMethod]
        public async Task Set_Session_Item()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServer();

                server.ValidatingClientConnectionAsync += e =>
                {
                    // Don't validate anything. Just set some session items.
                    e.SessionItems["can_subscribe_x"] = true;
                    e.SessionItems["default_payload"] = "Hello World";
                    
                    return Task.CompletedTask;
                };
                
                server.InterceptingClientSubscriptionAsync += e =>
                {
                    if (e.TopicFilter.Topic == "x")
                    {
                        if (e.SessionItems["can_subscribe_x"] as bool? == false)
                        {
                            e.Response.ReasonCode = MqttSubscribeReasonCode.ImplementationSpecificError;
                        }
                    }
                    
                    return Task.CompletedTask;
                };

                server.InterceptingClientPublishAsync += e =>
                {
                    e.ApplicationMessage.Payload =
                        Encoding.UTF8.GetBytes(
                            e.SessionItems["default_payload"] as string ?? String.Empty);

                    return Task.CompletedTask;
                };

                string receivedPayload = null;

                var client = await testEnvironment.ConnectClient();
                client.ApplicationMessageReceivedAsync += e =>
                {
                    receivedPayload = e.ApplicationMessage.ConvertPayloadToString();
                    return Task.CompletedTask;
                };

                var subscribeResult = await client.SubscribeAsync("x");

                Assert.AreEqual(MqttClientSubscribeResultCode.GrantedQoS0, subscribeResult.Items[0].ResultCode);

                var client2 = await testEnvironment.ConnectClient();
                await client2.PublishAsync("x");

                await Task.Delay(1000);

                Assert.AreEqual("Hello World", receivedPayload);
            }
        }

        [TestMethod]
        public async Task Get_Session_Items_In_Status()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServer();

                server.ValidatingClientConnectionAsync += e =>
                {
                    // Don't validate anything. Just set some session items.
                    e.SessionItems["can_subscribe_x"] = true;
                    e.SessionItems["default_payload"] = "Hello World";
                    
                    return Task.CompletedTask;
                };

                await testEnvironment.ConnectClient();

                var sessionStatus = await testEnvironment.Server.GetSessionStatusAsync();
                var session = sessionStatus.First();

                Assert.AreEqual(true, session.Items["can_subscribe_x"]);
            }
        }

        [TestMethod]
        public async Task Manage_Session_MaxParallel()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                testEnvironment.IgnoreClientLogErrors = true;
                var serverOptions = new MqttServerOptionsBuilder();
                await testEnvironment.StartServer(serverOptions);

                var options = new MqttClientOptionsBuilder().WithClientId("1");

                var clients = await Task.WhenAll(Enumerable.Range(0, 10)
                    .Select(i => TryConnect(testEnvironment, options)));

                var connectedClients = clients.Where(c => c?.IsConnected ?? false).ToList();

                Assert.AreEqual(1, connectedClients.Count);
            }
        }
        
        [TestMethod]
        public async Task Fire_Deleted_Event()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                // Arrange client and server.
                var server = await testEnvironment.StartServer(o => o.WithPersistentSessions(false));
                var client = await testEnvironment.ConnectClient();

                // Arrange session status tracking.
                var status = await server.GetSessionStatusAsync();
                var clientStatus = status[0];

                var deletedEventFired = false;
                clientStatus.Deleted += (_, __) =>
                {
                    deletedEventFired = true;
                };
                
                // Act: Disconnect the client -> Event must be fired.
                await client.DisconnectAsync();

                await LongTestDelay();
                
                // Assert that the event was fired properly.
                Assert.IsTrue(deletedEventFired);
            }
        }
        
        [TestMethod]
        public async Task Inject_Application_Message()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                // Arrange client and server.
                var server = await testEnvironment.StartServer();
                var client = await testEnvironment.ConnectClient();
                var messageReceivedHandler = testEnvironment.CreateApplicationMessageHandler(client);
                
                // Arrange session status tracking.
                var status = await server.GetSessionStatusAsync();
                var clientStatus = status[0];

                // Act: Inject the application message.
                await clientStatus.EnqueueApplicationMessageAsync(new MqttApplicationMessageBuilder()
                    .WithTopic("injected_one").Build());

                await LongTestDelay();
                
                // Assert 
                Assert.AreEqual(1, messageReceivedHandler.ReceivedEventArgs.Count);
                Assert.AreEqual("injected_one", messageReceivedHandler.ReceivedEventArgs[0].ApplicationMessage.Topic);
            }
        }
        
        [DataTestMethod]
        [DataRow(MqttQualityOfServiceLevel.ExactlyOnce)]
        [DataRow(MqttQualityOfServiceLevel.AtLeastOnce)]
        public async Task Retry_If_Not_PubAck(MqttQualityOfServiceLevel qos)
        {
            long count = 0;
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer(o => o.WithPersistentSessions());
                
                var publisher = await testEnvironment.ConnectClient();
                
                var subscriber = await testEnvironment.ConnectClient(o => o
                    .WithClientId(qos.ToString())
                    .WithCleanSession(false));
                
                subscriber.ApplicationMessageReceivedAsync += c =>
                {
                    c.AutoAcknowledge = false;
                    ++count;
                    Console.WriteLine("process");
                    return Task.CompletedTask;
                };
                
                await subscriber.SubscribeAsync("#", qos);

                var pub = publisher.PublishAsync("a", null, qos);

                await Task.Delay(100);
                await subscriber.DisconnectAsync();
                await subscriber.ReconnectAsync();
                await Task.Delay(100);

                var res = await pub;

                Assert.AreEqual(MqttClientPublishReasonCode.Success, res.ReasonCode);
                Assert.AreEqual(2, count);
            }
        }

        async Task<IMqttClient> TryConnect(TestEnvironment testEnvironment, MqttClientOptionsBuilder options)
        {
            try
            {
                return await testEnvironment.ConnectClient(options);
            }
            catch
            {
                return null;
            }
        }
    }
}