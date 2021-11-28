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
        public Task Execute_Success_With_QoS_0_MQTT_V5_Use_ResponseTopic()
        {
            return Execute_Success_MQTT_V5(MqttQualityOfServiceLevel.AtMostOnce);
        }

        [TestMethod]
        public Task Execute_Success_With_QoS_1_MQTT_V5_Use_ResponseTopic()
        {
            return Execute_Success_MQTT_V5(MqttQualityOfServiceLevel.AtLeastOnce);
        }

        [TestMethod]
        public Task Execute_Success_With_QoS_2_MQTT_V5_Use_ResponseTopic()
        {
            return Execute_Success_MQTT_V5(MqttQualityOfServiceLevel.ExactlyOnce);
        }


        [TestMethod]
        [ExpectedException(typeof(MqttCommunicationTimedOutException))]
        public async Task Execute_Timeout()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                await testEnvironment.StartServer();

                var requestSender = await testEnvironment.ConnectClient();

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
                await testEnvironment.StartServer();

                var requestSender = await testEnvironment.ConnectClient();

                var rpcClient = await testEnvironment.ConnectRpcClient(new MqttRpcClientOptionsBuilder().WithTopicGenerationStrategy(new TestTopicStrategy()).Build());

                await rpcClient.ExecuteAsync(TimeSpan.FromSeconds(2), "ping", "", MqttQualityOfServiceLevel.AtMostOnce);
            }
        }

        async Task Execute_Success(MqttQualityOfServiceLevel qosLevel, MqttProtocolVersion protocolVersion)
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                await testEnvironment.StartServer();
                var responseSender = await testEnvironment.ConnectClient(new MqttClientOptionsBuilder().WithProtocolVersion(protocolVersion));
                await responseSender.SubscribeAsync("MQTTnet.RPC/+/ping", qosLevel);

                responseSender.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(async e =>
                {
                    await responseSender.PublishAsync(e.ApplicationMessage.Topic + "/response", "pong");
                });

                var requestSender = await testEnvironment.ConnectClient();

                using (var rpcClient = new MqttRpcClient(requestSender, new MqttRpcClientOptionsBuilder().Build()))
                {
                    var response = await rpcClient.ExecuteAsync(TimeSpan.FromSeconds(5), "ping", "", qosLevel);

                    Assert.AreEqual("pong", Encoding.UTF8.GetString(response));
                }
            }
        }

        async Task Execute_Success_MQTT_V5(MqttQualityOfServiceLevel qosLevel)
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                await testEnvironment.StartServer();
                var responseSender = await testEnvironment.ConnectClient(new MqttClientOptionsBuilder().WithProtocolVersion(MqttProtocolVersion.V500));
                await responseSender.SubscribeAsync("MQTTnet.RPC/+/ping", qosLevel);

                responseSender.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(async e =>
                {
                    await responseSender.PublishAsync(e.ApplicationMessage.ResponseTopic, "pong");
                });

                var requestSender = await testEnvironment.ConnectClient(new MqttClientOptionsBuilder().WithProtocolVersion(MqttProtocolVersion.V500));

                using (var rpcClient = new MqttRpcClient(requestSender, new MqttRpcClientOptionsBuilder().Build()))
                {
                    var response = await rpcClient.ExecuteAsync(TimeSpan.FromSeconds(5), "ping", "", qosLevel);

                    Assert.AreEqual("pong", Encoding.UTF8.GetString(response));
                }
            }
        }

        [TestMethod]
        public async Task Execute_Success_MQTT_V5_Mixed_Clients()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                await testEnvironment.StartServer();
                var responseSender = await testEnvironment.ConnectClient();
                await responseSender.SubscribeAsync("MQTTnet.RPC/+/ping", MqttQualityOfServiceLevel.AtMostOnce);

                responseSender.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(async e =>
                {
                    Assert.IsNull(e.ApplicationMessage.ResponseTopic);
                    await responseSender.PublishAsync(e.ApplicationMessage.Topic + "/response", "pong");
                });

                var requestSender = await testEnvironment.ConnectClient(new MqttClientOptionsBuilder().WithProtocolVersion(MqttProtocolVersion.V500));

                using (var rpcClient = new MqttRpcClient(requestSender, new MqttRpcClientOptionsBuilder().Build()))
                {
                    var response = await rpcClient.ExecuteAsync(TimeSpan.FromSeconds(5), "ping", "", MqttQualityOfServiceLevel.AtMostOnce);

                    Assert.AreEqual("pong", Encoding.UTF8.GetString(response));
                }
            }
        }

        [TestMethod]
        [ExpectedException(typeof(MqttCommunicationTimedOutException))]
        public async Task Execute_Timeout_MQTT_V5_Mixed_Clients()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                await testEnvironment.StartServer();
                var responseSender = await testEnvironment.ConnectClient();
                await responseSender.SubscribeAsync("MQTTnet.RPC/+/ping", MqttQualityOfServiceLevel.AtMostOnce);

                responseSender.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(async e =>
                {
                    Assert.IsNull(e.ApplicationMessage.ResponseTopic);
                });

                var requestSender = await testEnvironment.ConnectClient(new MqttClientOptionsBuilder().WithProtocolVersion(MqttProtocolVersion.V500));

                using (var rpcClient = new MqttRpcClient(requestSender, new MqttRpcClientOptionsBuilder().Build()))
                {
                    var response = await rpcClient.ExecuteAsync(TimeSpan.FromSeconds(2), "ping", "", MqttQualityOfServiceLevel.AtMostOnce);
                }
            }
        }

        class TestTopicStrategy : IMqttRpcClientTopicGenerationStrategy
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
