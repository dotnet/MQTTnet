using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Client.Receiving;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace MQTTnet.Tests
{
    [TestClass]
    public sealed class Server_Events_Tests : BaseTestClass
    {
        [TestMethod]
        public async Task Fire_Client_Connected_Event()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServerAsync();

                MqttServerClientConnectedEventArgs eventArgs = null;
                server.ClientConnectedHandler = new MqttServerClientConnectedHandlerDelegate(e =>
                {
                    eventArgs = e;
                });

                await testEnvironment.ConnectClientAsync(o => o.WithCredentials("TheUser"));
                
                await LongDelay();
                
                Assert.IsNotNull(eventArgs);
                
                Assert.IsTrue(eventArgs.ClientId.StartsWith(nameof(Fire_Client_Connected_Event)));
                Assert.IsTrue(eventArgs.Endpoint.Contains("::1"));
                Assert.AreEqual(MqttProtocolVersion.V311, eventArgs.ProtocolVersion);
                Assert.AreEqual("TheUser", eventArgs.UserName);
            }
        }

        [TestMethod]
        public async Task Fire_Client_Disconnected_Event()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServerAsync();

                MqttServerClientDisconnectedEventArgs eventArgs = null;
                server.ClientDisconnectedHandler = new MqttServerClientDisconnectedHandlerDelegate(e =>
                {
                    eventArgs = e;
                });

                var client = await testEnvironment.ConnectClientAsync(o => o.WithCredentials("TheUser"));
                await client.DisconnectAsync();
                
                await LongDelay();
                
                Assert.IsNotNull(eventArgs);
                
                Assert.IsTrue(eventArgs.ClientId.StartsWith(nameof(Fire_Client_Disconnected_Event)));
                Assert.IsTrue(eventArgs.Endpoint.Contains("::1"));
                Assert.AreEqual(MqttClientDisconnectType.Clean, eventArgs.DisconnectType);
            }
        }
        
        [TestMethod]
        public async Task Fire_Client_Subscribed_Event()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServerAsync();

                MqttServerClientSubscribedTopicEventArgs eventArgs = null;
                server.ClientSubscribedTopicHandler = new MqttServerClientSubscribedTopicHandlerDelegate(e =>
                {
                    eventArgs = e;
                });

                var client = await testEnvironment.ConnectClientAsync();
                await client.SubscribeAsync("The/Topic", MqttQualityOfServiceLevel.AtLeastOnce);
                
                await LongDelay();
                
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
                var server = await testEnvironment.StartServerAsync();
        
                MqttServerClientUnsubscribedTopicEventArgs eventArgs = null;
                server.ClientUnsubscribedTopicHandler = new MqttServerClientUnsubscribedTopicHandlerDelegate(e =>
                {
                    eventArgs = e;
                });
        
                var client = await testEnvironment.ConnectClientAsync();
                await client.UnsubscribeAsync("The/Topic");
                
                await LongDelay();
                
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
                var server = await testEnvironment.StartServerAsync();
        
                MqttApplicationMessageReceivedEventArgs eventArgs = null;
                server.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(e =>
                {
                    eventArgs = e;
                });
        
                var client = await testEnvironment.ConnectClientAsync();
                await client.PublishAsync("The_Topic", "The_Payload");
                
                await LongDelay();
                
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
        
                await LongDelay();
                
                Assert.IsNotNull(eventArgs);
            }
        }
        
        [TestMethod]
        public async Task Fire_Stopped_Event()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServerAsync();
        
                EventArgs eventArgs = null;
                server.StoppedHandler = new MqttServerStoppedHandlerDelegate(e =>
                {
                    eventArgs = e;
                });

                await server.StopAsync();
        
                await LongDelay();
                
                Assert.IsNotNull(eventArgs);
            }
        }
    }
}