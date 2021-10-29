using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Tests.Server
{
    [TestClass]
    public sealed class Retained_Messages_Tests : BaseTestClass
    {
        [TestMethod]
        public async Task Server_Reports_Retained_Messages_Supported_V3()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer();

                var client = testEnvironment.CreateClient();
                
                var connectResult = await client.ConnectAsync(testEnvironment.Factory.CreateClientOptionsBuilder()
                    .WithProtocolVersion(MqttProtocolVersion.V311)
                    .WithTcpServer("127.0.0.1", testEnvironment.ServerPort).Build());

                Assert.IsTrue(connectResult.RetainAvailable);
            }
        }
        
        [TestMethod]
        public async Task Server_Reports_Retained_Messages_Supported_V5()
        {
            using (var testEnvironment = CreateTestEnvironment(MqttProtocolVersion.V500))
            {
                await testEnvironment.StartServer();

                var client = testEnvironment.CreateClient();
                var connectResult = await client.ConnectAsync(testEnvironment.Factory.CreateClientOptionsBuilder()
                    .WithProtocolVersion(MqttProtocolVersion.V500)
                    .WithTcpServer("127.0.0.1", testEnvironment.ServerPort).Build());

                Assert.IsTrue(connectResult.RetainAvailable);
            }
        }
        
        [TestMethod]
        public async Task Retained_Messages_Flow()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var retainedMessage = new MqttApplicationMessageBuilder().WithTopic("r").WithPayload("r").WithRetainFlag().Build();

                await testEnvironment.StartServer();
                var c1 = await testEnvironment.ConnectClient();
    
                var c2 = await testEnvironment.ConnectClient();
                var messageHandler = testEnvironment.CreateApplicationMessageHandler(c2);
                
                await c1.PublishAsync(retainedMessage);
                await c1.DisconnectAsync();
                await LongTestDelay();

                for (var i = 0; i < 5; i++)
                {
                    await c2.UnsubscribeAsync("r");
                    await Task.Delay(100);
                    messageHandler.AssertReceivedCountEquals(i);
                    
                    await c2.SubscribeAsync("r");
                    await Task.Delay(100);
                    messageHandler.AssertReceivedCountEquals(i + 1);
                }

                await c2.DisconnectAsync();
            }
        }

        [TestMethod]
        public async Task Receive_No_Retained_Message_After_Subscribe()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer();

                var c1 = await testEnvironment.ConnectClient();
                await c1.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("retained").WithPayload(new byte[3]).WithRetainFlag().Build());
                await c1.DisconnectAsync();

                var c2 = await testEnvironment.ConnectClient();
                var messageHandler = testEnvironment.CreateApplicationMessageHandler(c2);
                await c2.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("retained_other").Build());

                await Task.Delay(500);

                messageHandler.AssertReceivedCountEquals(0);
            }
        }

        [TestMethod]
        public async Task Receive_Retained_Message_After_Subscribe()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer();

                var c1 = await testEnvironment.ConnectClient();
                await c1.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("retained").WithPayload(new byte[3]).WithRetainFlag().Build());
                await c1.DisconnectAsync();

                var c2 = await testEnvironment.ConnectClient();
                var messageHandler = testEnvironment.CreateApplicationMessageHandler(c2);
                
                await c2.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("retained").Build());

                await Task.Delay(500);

                messageHandler.AssertReceivedCountEquals(1);
                Assert.IsTrue(messageHandler.ReceivedEventArgs.First().ApplicationMessage.Retain);
            }
        }

        [TestMethod]
        public async Task Clear_Retained_Message_With_Empty_Payload()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer();

                var c1 = await testEnvironment.ConnectClient();

                await c1.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("retained").WithPayload(new byte[3]).WithRetainFlag().Build());
                await c1.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("retained").WithPayload(new byte[0]).WithRetainFlag().Build());

                await c1.DisconnectAsync();

                var c2 = await testEnvironment.ConnectClient();
                var messageHandler = testEnvironment.CreateApplicationMessageHandler(c2);
                
                await Task.Delay(200);
                await c2.SubscribeAsync(new MqttTopicFilter { Topic = "retained", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce });
                await Task.Delay(500);

                messageHandler.AssertReceivedCountEquals(0);
            }
        }

        [TestMethod]
        public async Task Clear_Retained_Message_With_Null_Payload()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer();

                var c1 = await testEnvironment.ConnectClient();

                await c1.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("retained").WithPayload(new byte[3]).WithRetainFlag().Build());
                await c1.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("retained").WithPayload((byte[])null).WithRetainFlag().Build());

                await c1.DisconnectAsync();

                var c2 = await testEnvironment.ConnectClient();
                var messageHandler = testEnvironment.CreateApplicationMessageHandler(c2);
                
                await Task.Delay(200);
                await c2.SubscribeAsync(new MqttTopicFilter { Topic = "retained", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce });
                await Task.Delay(500);

                messageHandler.AssertReceivedCountEquals(0);
            }
        }
    }
}