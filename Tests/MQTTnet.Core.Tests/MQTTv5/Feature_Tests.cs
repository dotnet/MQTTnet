using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Client.Subscribing;
using MQTTnet.Formatter;
using MQTTnet.Tests.Mockups;

namespace MQTTnet.Tests.MQTTv5
{
    [TestClass]
    public class Feature_Tests
    {
        [TestMethod]
        public async Task Use_User_Properties()
        {
            using (var testEnvironment = new TestEnvironment())
            {
                await testEnvironment.StartServerAsync();

                var sender = await testEnvironment.ConnectClientAsync(new MqttClientOptionsBuilder().WithProtocolVersion(MqttProtocolVersion.V500));
                var receiver = await testEnvironment.ConnectClientAsync(new MqttClientOptionsBuilder().WithProtocolVersion(MqttProtocolVersion.V500));

                var message = new MqttApplicationMessageBuilder()
                    .WithTopic("A")
                    .WithUserProperty("x", "1")
                    .WithUserProperty("y", "2")
                    .WithUserProperty("z", "3")
                    .WithUserProperty("z", "4"); // z is here two times to test list of items

                await receiver.SubscribeAsync(new MqttClientSubscribeOptions
                {
                    TopicFilters = new List<TopicFilter>
                    {
                        new TopicFilter { Topic = "#" }
                    }
                }, CancellationToken.None);

                MqttApplicationMessage receivedMessage = null;
                receiver.UseApplicationMessageReceivedHandler(e => receivedMessage = e.ApplicationMessage);

                await sender.PublishAsync(message.Build(), CancellationToken.None);

                await Task.Delay(1000);

                Assert.IsNotNull(receivedMessage);
                Assert.AreEqual("A", receivedMessage.Topic);
                Assert.AreEqual(4, receivedMessage.UserProperties.Count);

            }
        }
    }
}
