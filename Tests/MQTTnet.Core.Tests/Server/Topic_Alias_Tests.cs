using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Formatter;

namespace MQTTnet.Tests.Server
{
    [TestClass]
    public sealed class Topic_Alias_Tests : BaseTestClass
    {
        [TestMethod]
        public async Task Server_Reports_Topic_Alias_Supported()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer();
                
                var client = testEnvironment.CreateClient();
                
                var connectResult = await client.ConnectAsync(new MqttClientOptionsBuilder()
                    .WithProtocolVersion(MqttProtocolVersion.V500)
                    .WithTcpServer("127.0.0.1", testEnvironment.ServerPort)
                    .Build());
                
                Assert.AreEqual(connectResult.TopicAliasMaximum, ushort.MaxValue);
            }
        }
        
        [TestMethod]
        public async Task Publish_With_Topic_Alias()
        {
            using (var testEnvironment = CreateTestEnvironment())
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
