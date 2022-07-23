using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Implementations;
using MQTTnet.Protocol;

namespace MQTTnet.Tests.Server
{
    [TestClass]
    public sealed class Will_Tests : BaseTestClass
    {
        [TestMethod]
        public async Task Will_Message_Do_Not_Send_On_Clean_Disconnect()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var receivedMessagesCount = 0;

                await testEnvironment.StartServer();

                var clientOptions = new MqttClientOptionsBuilder().WithWillTopic("My/last/will");

                var c1 = await testEnvironment.ConnectClient();

                c1.ApplicationMessageReceivedAsync += e =>
                {
                    Interlocked.Increment(ref receivedMessagesCount);
                    return PlatformAbstractionLayer.CompletedTask;
                };

                await c1.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("#").Build());

                var c2 = await testEnvironment.ConnectClient(clientOptions);
                await c2.DisconnectAsync().ConfigureAwait(false);

                await Task.Delay(1000);

                Assert.AreEqual(0, receivedMessagesCount);
            }
        }

        [TestMethod]
        public async Task Will_Message_Send()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer();

                var c1 = await testEnvironment.ConnectClient(new MqttClientOptionsBuilder());

                var receivedMessagesCount = 0;
                c1.ApplicationMessageReceivedAsync += e =>
                {
                    Interlocked.Increment(ref receivedMessagesCount);
                    return PlatformAbstractionLayer.CompletedTask;
                };

                await c1.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("#").Build());

                var clientOptions = new MqttClientOptionsBuilder().WithWillTopic("My/last/will").WithWillQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce);
                var c2 = await testEnvironment.ConnectClient(clientOptions);
                c2.Dispose(); // Dispose will not send a DISCONNECT pattern first so the will message must be sent.

                await Task.Delay(1000);

                Assert.AreEqual(1, receivedMessagesCount);
            }
        }
    }
}