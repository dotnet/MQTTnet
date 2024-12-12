// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Formatter;
using MQTTnet.Internal;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MQTTnet.Tests.Mockups;

namespace MQTTnet.Tests.Server
{
    [TestClass]
    public sealed class Events_Tests : BaseTestClass
    {
        [TestMethod]
        public async Task Fire_Client_Connected_Event()
        {
            using var testEnvironments = CreateMixedTestEnvironment();
            foreach (var testEnvironment in testEnvironments)
            {
                var server = await testEnvironment.StartServer();
                var eventArgsTaskSource = new TaskCompletionSource<ClientConnectedEventArgs>();
                 
                server.ClientConnectedAsync += e =>
                {
                    eventArgsTaskSource.TrySetResult(e);
                    return CompletedTask.Instance;
                };

                await testEnvironment.ConnectClient(o => o.WithCredentials("TheUser", "ThePassword"));

                var eventArgs = await eventArgsTaskSource.Task.WaitAsync(TimeSpan.FromSeconds(10d));

                Assert.IsNotNull(eventArgs);

                Assert.IsTrue(eventArgs.ClientId.StartsWith(nameof(Fire_Client_Connected_Event)));
                Assert.IsTrue(eventArgs.RemoteEndPoint.ToString().Contains("127.0.0.1"));
                Assert.AreEqual(MqttProtocolVersion.V311, eventArgs.ProtocolVersion);
                Assert.AreEqual("TheUser", eventArgs.UserName);
                Assert.AreEqual("ThePassword", eventArgs.Password);
            }
        }

        [TestMethod]
        public async Task Fire_Client_Disconnected_Event()
        {
            using var testEnvironments = CreateMixedTestEnvironment();
            foreach (var testEnvironment in testEnvironments)
            {
                var server = await testEnvironment.StartServer();

                var eventArgsTaskSource = new TaskCompletionSource<ClientDisconnectedEventArgs>();

                server.ClientDisconnectedAsync += e =>
                {
                    eventArgsTaskSource.TrySetResult(e);
                    return CompletedTask.Instance;
                };

                var client = await testEnvironment.ConnectClient(o => o.WithCredentials("TheUser", "ThePassword"));
                await client.DisconnectAsync();

                var eventArgs = await eventArgsTaskSource.Task.WaitAsync(TimeSpan.FromSeconds(10d));

                Assert.IsNotNull(eventArgs);

                Assert.IsTrue(eventArgs.ClientId.StartsWith(nameof(Fire_Client_Disconnected_Event)));
                Assert.IsTrue(eventArgs.RemoteEndPoint.ToString().Contains("127.0.0.1"));
                Assert.AreEqual(MqttClientDisconnectType.Clean, eventArgs.DisconnectType);

                Assert.AreEqual("TheUser", eventArgs.UserName);
                Assert.AreEqual("ThePassword", eventArgs.Password);
            }
        }

        [TestMethod]
        public async Task Fire_Client_Subscribed_Event()
        {
            using var testEnvironments = CreateMixedTestEnvironment();
            foreach (var testEnvironment in testEnvironments)
            {
                var server = await testEnvironment.StartServer();

                var eventArgsTaskSource = new TaskCompletionSource<ClientSubscribedTopicEventArgs>();
                 
                server.ClientSubscribedTopicAsync += e =>
                {
                    eventArgsTaskSource.TrySetResult(e);
                    return CompletedTask.Instance;
                };

                var client = await testEnvironment.ConnectClient(o => o.WithCredentials("TheUser"));
                await client.SubscribeAsync("The/Topic", MqttQualityOfServiceLevel.AtLeastOnce);

                var eventArgs = await eventArgsTaskSource.Task.WaitAsync(TimeSpan.FromSeconds(10d));

                Assert.IsNotNull(eventArgs);

                Assert.IsTrue(eventArgs.ClientId.StartsWith(nameof(Fire_Client_Subscribed_Event)));
                Assert.AreEqual("The/Topic", eventArgs.TopicFilter.Topic);
                Assert.AreEqual(MqttQualityOfServiceLevel.AtLeastOnce, eventArgs.TopicFilter.QualityOfServiceLevel);
                Assert.AreEqual("TheUser", eventArgs.UserName);
            }
        }

        [TestMethod]
        public async Task Fire_Client_Unsubscribed_Event()
        {
            using var testEnvironments = CreateMixedTestEnvironment();
            foreach (var testEnvironment in testEnvironments)
            {
                var server = await testEnvironment.StartServer();

                var eventArgsTaskSource = new TaskCompletionSource<ClientUnsubscribedTopicEventArgs>();
             
                server.ClientUnsubscribedTopicAsync += e =>
                {
                    eventArgsTaskSource.TrySetResult(e);
                    return CompletedTask.Instance;
                };

                var client = await testEnvironment.ConnectClient(o => o.WithCredentials("TheUser"));
                await client.UnsubscribeAsync("The/Topic");

                var eventArgs = await eventArgsTaskSource.Task.WaitAsync(TimeSpan.FromSeconds(10d));

                Assert.IsNotNull(eventArgs);

                Assert.IsTrue(eventArgs.ClientId.StartsWith(nameof(Fire_Client_Unsubscribed_Event)));
                Assert.AreEqual("The/Topic", eventArgs.TopicFilter);
                Assert.AreEqual("TheUser", eventArgs.UserName);
            }
        }

        [TestMethod]
        public async Task Fire_Application_Message_Received_Event()
        {
            using var testEnvironments = CreateMixedTestEnvironment();
            foreach (var testEnvironment in testEnvironments)
            {
                var server = await testEnvironment.StartServer();

                var eventArgsTaskSource = new TaskCompletionSource<InterceptingPublishEventArgs>();

                server.InterceptingPublishAsync += e =>
                {
                    eventArgsTaskSource.TrySetResult(e);
                    return CompletedTask.Instance;
                };

                var client = await testEnvironment.ConnectClient(o => o.WithCredentials("TheUser"));
                await client.PublishStringAsync("The_Topic", "The_Payload");

                var eventArgs = await eventArgsTaskSource.Task.WaitAsync(TimeSpan.FromSeconds(10d));

                Assert.IsNotNull(eventArgs);

                Assert.IsTrue(eventArgs.ClientId.StartsWith(nameof(Fire_Application_Message_Received_Event)));
                Assert.AreEqual("The_Topic", eventArgs.ApplicationMessage.Topic);
                Assert.AreEqual("The_Payload", eventArgs.ApplicationMessage.ConvertPayloadToString());
                Assert.AreEqual("TheUser", eventArgs.UserName);
            }
        }

        [TestMethod]
        public async Task Fire_Started_Event()
        {
            using var testEnvironments = CreateMQTTnetTestEnvironment();
            foreach (var testEnvironment in testEnvironments)
            {
                var server = testEnvironment.CreateServer(new MqttServerOptions());

                var eventArgsTaskSource = new TaskCompletionSource<EventArgs>();
                server.StartedAsync += e =>
                {
                    eventArgsTaskSource.TrySetResult(e);
                    return CompletedTask.Instance;
                };

                await server.StartAsync();

                var eventArgs = await eventArgsTaskSource.Task.WaitAsync(TimeSpan.FromSeconds(10d));

                Assert.IsNotNull(eventArgs);
            }
        }

        [TestMethod]
        public async Task Fire_Stopped_Event()
        {
            using var testEnvironments = CreateMixedTestEnvironment();
            foreach (var testEnvironment in testEnvironments)
            {
                var server = await testEnvironment.StartServer();
                var eventArgsTaskSource = new TaskCompletionSource<EventArgs>();
                 
                server.StoppedAsync += e =>
                {
                    eventArgsTaskSource.TrySetResult(e);
                    return CompletedTask.Instance;
                };

                await server.StopAsync();

                var eventArgs = await eventArgsTaskSource.Task.WaitAsync(TimeSpan.FromSeconds(10d));

                Assert.IsNotNull(eventArgs);
            }
        }
    }
}