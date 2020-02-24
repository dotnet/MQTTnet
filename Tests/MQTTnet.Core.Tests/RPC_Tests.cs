using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Tests.Mockups;
using MQTTnet.Client;
using MQTTnet.Client.Receiving;
using MQTTnet.Exceptions;
using MQTTnet.Extensions.Rpc;
using MQTTnet.Protocol;
using MQTTnet.Client.Options;
using MQTTnet.Formatter;
using MQTTnet.Extensions.Rpc.Options;
using MQTTnet.Extensions.Rpc.Options.TopicGeneration;

namespace MQTTnet.Tests
{
    [TestClass]
    public class RPC_Tests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public Task Execute_Success_With_QoS_0()
        {
            return Execute_Success(MqttQualityOfServiceLevel.AtMostOnce, MqttProtocolVersion.V311);
        }

        [TestMethod]
        public Task Execute_Success_With_QoS_1()
        {
            return Execute_Success(MqttQualityOfServiceLevel.AtLeastOnce, MqttProtocolVersion.V311);
        }

        [TestMethod]
        public Task Execute_Success_With_QoS_2()
        {
            return Execute_Success(MqttQualityOfServiceLevel.ExactlyOnce, MqttProtocolVersion.V311);
        }

        [TestMethod]
        public Task Execute_Success_With_QoS_0_MQTT_V5()
        {
            return Execute_Success(MqttQualityOfServiceLevel.AtMostOnce, MqttProtocolVersion.V500);
        }

        [TestMethod]
        public Task Execute_Success_With_QoS_1_MQTT_V5()
        {
            return Execute_Success(MqttQualityOfServiceLevel.AtLeastOnce, MqttProtocolVersion.V500);
        }

        [TestMethod]
        public Task Execute_Success_With_QoS_2_MQTT_V5()
        {
            return Execute_Success(MqttQualityOfServiceLevel.ExactlyOnce, MqttProtocolVersion.V500);
        }

        [TestMethod]
        [ExpectedException(typeof(MqttCommunicationTimedOutException))]
        public async Task Execute_Timeout()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                await testEnvironment.StartServerAsync();
                
                var requestSender = await testEnvironment.ConnectClientAsync();

                var rpcClient = new MqttRpcClient(requestSender, new MqttRpcClientOptionsBuilder().Build());
                await rpcClient.ExecuteAsync(TimeSpan.FromSeconds(2), "ping", "", MqttQualityOfServiceLevel.AtMostOnce);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(MqttCommunicationTimedOutException))]
        public async Task Execute_With_Custom_Topic_Names()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                await testEnvironment.StartServerAsync();

                var requestSender = await testEnvironment.ConnectClientAsync();

                var rpcClient = new MqttRpcClient(requestSender, new MqttRpcClientOptionsBuilder().WithTopicGenerationStrategy(new TestTopicStrategy()) .Build());
                await rpcClient.ExecuteAsync(TimeSpan.FromSeconds(2), "ping", "", MqttQualityOfServiceLevel.AtMostOnce);
            }
        }

        private async Task Execute_Success(MqttQualityOfServiceLevel qosLevel, MqttProtocolVersion protocolVersion)
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                await testEnvironment.StartServerAsync();
                var responseSender = await testEnvironment.ConnectClientAsync(new MqttClientOptionsBuilder().WithProtocolVersion(protocolVersion));
                await responseSender.SubscribeAsync("MQTTnet.RPC/+/ping");

                responseSender.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(async e =>
                {
                    await responseSender.PublishAsync(e.ApplicationMessage.Topic + "/response", "pong");
                });

                var requestSender = await testEnvironment.ConnectClientAsync();

                var rpcClient = new MqttRpcClient(requestSender, new MqttRpcClientOptionsBuilder().Build());
                var response = await rpcClient.ExecuteAsync(TimeSpan.FromSeconds(5), "ping", "", qosLevel);

                Assert.AreEqual("pong", Encoding.UTF8.GetString(response));
            }
        }

        private class TestTopicStrategy : IMqttRpcClientTopicGenerationStrategy
        {
            public MqttRpcTopicPair CreateRpcTopics(TopicGenerationContext context)
            {
                return new MqttRpcTopicPair
                {
                    RequestTopic = "a",
                    ResponseTopic = "b"
                };
            }
        }
    }
}
