using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Client.Receiving;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace MQTTnet.Tests.Server
{
    [TestClass]
    public sealed class Events_Tests : BaseTestClass
    {
        [TestMethod]
        public async Task Fire_Client_Connected_Event()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServer();

                MqttServerClientConnectedEventArgs eventArgs = null;
                server.ClientConnectedHandler = new MqttServerClientConnectedHandlerDelegate(e =>
                {
                    eventArgs = e;
                });

                await testEnvironment.ConnectClient(o => o.WithCredentials("TheUser"));
                
                await LongTestDelay();
                
                Assert.IsNotNull(eventArgs);
                
                Assert.IsTrue(eventArgs.ClientId.StartsWith(nameof(Fire_Client_Connected_Event)));
                Assert.IsTrue(eventArgs.Endpoint.Contains("127.0.0.1"));
                Assert.AreEqual(MqttProtocolVersion.V311, eventArgs.ProtocolVersion);
                Assert.AreEqual("TheUser", eventArgs.UserName);
            }
        }

        [TestMethod]
        public async Task Fire_Client_Disconnected_Event()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServer();

                MqttServerClientDisconnectedEventArgs eventArgs = null;
                server.ClientDisconnectedHandler = new MqttServerClientDisconnectedHandlerDelegate(e =>
                {
                    eventArgs = e;
                });

                var client = await testEnvironment.ConnectClient(o => o.WithCredentials("TheUser"));
                await client.DisconnectAsync();
                
                await LongTestDelay();
                
                Assert.IsNotNull(eventArgs);
                
                Assert.IsTrue(eventArgs.ClientId.StartsWith(nameof(Fire_Client_Disconnected_Event)));
                Assert.IsTrue(eventArgs.Endpoint.Contains("127.0.0.1"));
                Assert.AreEqual(MqttClientDisconnectType.Clean, eventArgs.DisconnectType);
            }
        }
        
        [TestMethod]
        public async Task Fire_Client_Subscribed_Event()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServer();

                MqttServerClientSubscribedTopicEventArgs eventArgs = null;
                server.ClientSubscribedTopicHandler = new MqttServerClientSubscribedTopicHandlerDelegate(e =>
                {
                    eventArgs = e;
                });

                var client = await testEnvironment.ConnectClient();
                await client.SubscribeAsync("The/Topic", MqttQualityOfServiceLevel.AtLeastOnce);
                
                await LongTestDelay();
                
                Assert.IsNotNull(eventArgs);
                
                Assert.IsTrue(eventArgs.ClientId.StartsWith(nameof(Fire_Client_Subscribed_Event)));
                Assert.AreEqual("The/Topic", eventArgs.TopicFilter.Topic);
                Assert.AreEqual(MqttQualityOfServiceLevel.AtLeastOnce, eventArgs.TopicFilter.QualityOfServiceLevel);
            }
        }
        
        [TestMethod]
        public async Task Fire_Client_Unsubscribed_Event()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServer();
        
                MqttServerClientUnsubscribedTopicEventArgs eventArgs = null;
                server.ClientUnsubscribedTopicHandler = new MqttServerClientUnsubscribedTopicHandlerDelegate(e =>
                {
                    eventArgs = e;
                });
        
                var client = await testEnvironment.ConnectClient();
                await client.UnsubscribeAsync("The/Topic");
                
                await LongTestDelay();
                
                Assert.IsNotNull(eventArgs);
                
                Assert.IsTrue(eventArgs.ClientId.StartsWith(nameof(Fire_Client_Unsubscribed_Event)));
                Assert.AreEqual("The/Topic", eventArgs.TopicFilter);
            }
        }
        
        [TestMethod]
        public async Task Fire_Application_Message_Received_Event()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServer();
        
                MqttApplicationMessageReceivedEventArgs eventArgs = null;
                server.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(e =>
                {
                    eventArgs = e;
                });
        
                var client = await testEnvironment.ConnectClient();
                await client.PublishAsync("The_Topic", "The_Payload");
                
                await LongTestDelay();
                
                Assert.IsNotNull(eventArgs);
                
                Assert.IsTrue(eventArgs.ClientId.StartsWith(nameof(Fire_Application_Message_Received_Event)));
                Assert.AreEqual("The_Topic", eventArgs.ApplicationMessage.Topic);
                Assert.AreEqual("The_Payload", eventArgs.ApplicationMessage.ConvertPayloadToString());
            }
        }
        
        [TestMethod]
        public async Task Fire_Started_Event()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = testEnvironment.CreateServer();
        
                EventArgs eventArgs = null;
                server.StartedHandler = new MqttServerStartedHandlerDelegate(e =>
                {
                    eventArgs = e;
                });

                await server.StartAsync(new MqttServerOptionsBuilder().Build());
        
                await LongTestDelay();
                
                Assert.IsNotNull(eventArgs);
            }
        }
        
        [TestMethod]
        public async Task Fire_Stopped_Event()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServer();
        
                EventArgs eventArgs = null;
                server.StoppedHandler = new MqttServerStoppedHandlerDelegate(e =>
                {
                    eventArgs = e;
                });

                await server.StopAsync();
        
                await LongTestDelay();
                
                Assert.IsNotNull(eventArgs);
            }
        }
    }
}