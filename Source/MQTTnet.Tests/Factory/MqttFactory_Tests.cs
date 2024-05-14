// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Diagnostics;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Internal;
using MQTTnet.Server;

namespace MQTTnet.Tests.Factory
{
    [TestClass]
    public sealed class MqttFactory_Tests : BaseTestClass
    {
        [TestMethod]
        public async Task Create_Managed_Client_With_Logger()
        {
            var factory = new MqttClientFactory();

            // This test compares
            // 1. correct logID
            var logId = "logId";
            var hasInvalidLogId = false;

            // 2. if the total log calls are the same for global and local
            //var globalLogCount = 0;
            var localLogCount = 0;

            var logger = new MqttNetEventLogger(logId);
            logger.LogMessagePublished += (s, e) =>
            {
                if (e.LogMessage.LogId != logId)
                {
                    hasInvalidLogId = true;
                }

                Interlocked.Increment(ref localLogCount);
            };

            var managedClient = factory.CreateManagedMqttClient(logger);
            try
            {
                var clientOptions = new ManagedMqttClientOptionsBuilder();

                clientOptions.WithClientOptions(o => o.WithTcpServer("this_is_an_invalid_host").WithTimeout(TimeSpan.FromSeconds(1)));

                // try connect to get some log entries
                await managedClient.StartAsync(clientOptions.Build());

                // wait at least connect timeout or we have some log messages
                var tcs = new TaskCompletionSource<object>();
                managedClient.ConnectingFailedAsync += e =>
                {
                    tcs.TrySetResult(null);
                    return CompletedTask.Instance;
                };

                await Task.WhenAny(Task.Delay(managedClient.Options.ClientOptions.Timeout), tcs.Task);
            }
            finally
            {
                await managedClient.StopAsync();
            }

            await Task.Delay(500);

            Assert.IsFalse(hasInvalidLogId);
            Assert.AreNotEqual(0, localLogCount);
        }

        [TestMethod]
        public void Create_ApplicationMessageBuilder()
        {
            var factory = new MqttClientFactory();
            var builder = factory.CreateApplicationMessageBuilder();

            Assert.IsNotNull(builder);
        }

        [TestMethod]
        public void Create_ClientOptionsBuilder()
        {
            var factory = new MqttClientFactory();
            var builder = factory.CreateClientOptionsBuilder();

            Assert.IsNotNull(builder);
        }

        [TestMethod]
        public void Create_ServerOptionsBuilder()
        {
            var factory = new MqttServerFactory();
            var builder = factory.CreateServerOptionsBuilder();

            Assert.IsNotNull(builder);
        }

        [TestMethod]
        public void Create_SubscribeOptionsBuilder()
        {
            var factory = new MqttClientFactory();
            var builder = factory.CreateSubscribeOptionsBuilder();

            Assert.IsNotNull(builder);
        }

        [TestMethod]
        public void Create_UnsubscribeOptionsBuilder()
        {
            var factory = new MqttClientFactory();
            var builder = factory.CreateUnsubscribeOptionsBuilder();

            Assert.IsNotNull(builder);
        }

        [TestMethod]
        public void Create_TopicFilterBuilder()
        {
            var factory = new MqttClientFactory();
            var builder = factory.CreateTopicFilterBuilder();

            Assert.IsNotNull(builder);
        }

        [TestMethod]
        public void Create_MqttServer()
        {
            var factory = new MqttServerFactory();
            var server = factory.CreateMqttServer(new MqttServerOptionsBuilder().Build());

            Assert.IsNotNull(server);
        }

        [TestMethod]
        public void Create_MqttClient()
        {
            var factory = new MqttClientFactory();
            var client = factory.CreateMqttClient();

            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void Create_LowLevelMqttClient()
        {
            var factory = new MqttClientFactory();
            var client = factory.CreateLowLevelMqttClient();

            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void Create_ManagedMqttClient()
        {
            var factory = new MqttClientFactory();
            var client = factory.CreateManagedMqttClient();

            Assert.IsNotNull(client);
        }
    }
}