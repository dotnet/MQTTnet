using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Formatter;
using MQTTnet.Tests.Mockups;

namespace MQTTnet.Tests.Server
{
    [TestClass]
    public class Topic_Alias_Tests
    {
        [TestMethod]
        public async Task Publish_After_Client_Connects()
        {
            using (var testEnvironment = new TestEnvironment())
            {
                await testEnvironment.StartServer();

                var receivedTopics = new List<string>();

                var c1 = await testEnvironment.ConnectClient(options => options.WithProtocolVersion(MqttProtocolVersion.V500));
                c1.UseApplicationMessageReceivedHandler(e =>
                {
                    lock (receivedTopics)
                    {
                        receivedTopics.Add(e.ApplicationMessage.Topic);
                    }
                });

                await c1.SubscribeAsync("#");

                var c2 = await testEnvironment.ConnectClient(options => options.WithProtocolVersion(MqttProtocolVersion.V500));

                var message = new MqttApplicationMessage
                {
                    Topic = "this_is_the_topic",
                    TopicAlias = 22
                };

                await c2.PublishAsync(message);

                message.Topic = null;

                await c2.PublishAsync(message);
                await c2.PublishAsync(message);

                await Task.Delay(500);

                Assert.AreEqual(3, receivedTopics.Count);
                CollectionAssert.AllItemsAreNotNull(receivedTopics);
                Assert.IsTrue(receivedTopics.All(t => t.Equals("this_is_the_topic")));
            }
        }
    }
}
