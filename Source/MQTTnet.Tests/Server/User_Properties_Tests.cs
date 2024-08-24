// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Formatter;
using MQTTnet.Implementations;
using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Tests.Mockups;

namespace MQTTnet.Tests.Server
{
    [TestClass]
    public class Feature_Tests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public async Task Use_User_Properties()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                await testEnvironment.StartServer();

                var sender = await testEnvironment.ConnectClient(new MqttClientOptionsBuilder().WithProtocolVersion(MqttProtocolVersion.V500));
                var receiver = await testEnvironment.ConnectClient(new MqttClientOptionsBuilder().WithProtocolVersion(MqttProtocolVersion.V500));

                var message = new MqttApplicationMessageBuilder()
                    .WithTopic("A")
                    .WithUserProperty("x", "1")
                    .WithUserProperty("y", "2")
                    .WithUserProperty("z", "3")
                    .WithUserProperty("z", "4"); // z is here two times to test list of items

                await receiver.SubscribeAsync(new MqttClientSubscribeOptions
                {
                    TopicFilters = new List<MqttTopicFilter>
                    {
                        new MqttTopicFilter { Topic = "#" }
                    }
                }, CancellationToken.None);

                MqttApplicationMessage receivedMessage = null;
                receiver.ApplicationMessageReceivedAsync += e =>
                {
                    receivedMessage = e.ApplicationMessage;
                    return CompletedTask.Instance;
                };

                await sender.PublishAsync(message.Build(), CancellationToken.None);

                await Task.Delay(1000);

                Assert.IsNotNull(receivedMessage);
                Assert.AreEqual("A", receivedMessage.Topic);
                Assert.AreEqual(4, receivedMessage.UserProperties.Count);

            }
        }
    }
}
