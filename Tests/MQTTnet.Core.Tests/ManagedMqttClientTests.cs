using MQTTnet.Extensions.ManagedClient;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Server;

namespace MQTTnet.Core.Tests
{
    [TestClass]
    public class ManagedMqttClientTests
    {
        [TestMethod]
        public async Task Drop_New_Messages_On_Full_Queue()
        {
            var factory = new MqttFactory();
            var managedClient = factory.CreateManagedMqttClient();

            var clientOptions = new ManagedMqttClientOptionsBuilder()
                .WithMaxPendingMessages(5)
                .WithPendingMessagesOverflowStrategy(MqttPendingMessagesOverflowStrategy.DropNewMessage);

            clientOptions.WithClientOptions(o => o.WithTcpServer("localhost"));

            await managedClient.StartAsync(clientOptions.Build());

            await managedClient.PublishAsync(new MqttApplicationMessage { Topic = "1" });
            await managedClient.PublishAsync(new MqttApplicationMessage { Topic = "2" });
            await managedClient.PublishAsync(new MqttApplicationMessage { Topic = "3" });
            await managedClient.PublishAsync(new MqttApplicationMessage { Topic = "4" });
            await managedClient.PublishAsync(new MqttApplicationMessage { Topic = "5" });

            await managedClient.PublishAsync(new MqttApplicationMessage { Topic = "6" });
            await managedClient.PublishAsync(new MqttApplicationMessage { Topic = "7" });
            await managedClient.PublishAsync(new MqttApplicationMessage { Topic = "8" });

            Assert.AreEqual(5, managedClient.PendingApplicationMessagesCount);
        }
    }
}
