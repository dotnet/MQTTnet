// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using MQTTnet.Exceptions;
using MQTTnet.Extensions.Rpc;
using MQTTnet.Formatter;
using MQTTnet.Internal;
using MQTTnet.Protocol;
using MQTTnet.Tests.Mockups;

namespace MQTTnet.Tests.Extensions;

// ReSharper disable InconsistentNaming
[TestClass]
public sealed class Rpc_Tests : BaseTestClass
{
    [TestMethod]
    public async Task Execute_Success_MQTT_V5_Mixed_Clients()
    {
        using var testEnvironment = CreateTestEnvironment();
        await testEnvironment.StartServer();
        var responseSender = await testEnvironment.ConnectClient();
        await responseSender.SubscribeAsync("MQTTnet.RPC/+/ping");

        responseSender.ApplicationMessageReceivedAsync += async e =>
        {
            Assert.IsNull(e.ApplicationMessage.ResponseTopic);
            await responseSender.PublishStringAsync(e.ApplicationMessage.Topic + "/response", "pong").ConfigureAwait(false);
        };

        var requestSender = await testEnvironment.ConnectClient(new MqttClientOptionsBuilder().WithProtocolVersion(MqttProtocolVersion.V500));

        using var rpcClient = new MqttRpcClient(requestSender, new MqttRpcClientOptionsBuilder().Build());
        var response = await rpcClient.ExecuteAsync(TimeSpan.FromSeconds(5), "ping", "", MqttQualityOfServiceLevel.AtMostOnce);

        Assert.AreEqual("pong", Encoding.UTF8.GetString(response));
    }

    [TestMethod]
    public async Task Execute_Success_Parameters_Propagated_Correctly()
    {
        var paramValue = "123";
        var parameters = new Dictionary<string, object>
        {
            { TestParametersTopicGenerationStrategy.ExpectedParamName, "123" }
        };

        using var testEnvironment = CreateTestEnvironment();
        await testEnvironment.StartServer();

        var responseSender = await testEnvironment.ConnectClient(new MqttClientOptionsBuilder());
        await responseSender.SubscribeAsync($"MQTTnet.RPC/+/ping/{paramValue}");

        responseSender.ApplicationMessageReceivedAsync += e => responseSender.PublishStringAsync(e.ApplicationMessage.Topic + "/response", "pong");

        using var rpcClient = await testEnvironment.ConnectRpcClient(new MqttRpcClientOptionsBuilder()
            .WithTopicGenerationStrategy(new TestParametersTopicGenerationStrategy()).Build());
        var response = await rpcClient.ExecuteAsync(TimeSpan.FromSeconds(5), "ping", "", MqttQualityOfServiceLevel.AtMostOnce, parameters);

        Assert.AreEqual("pong", Encoding.UTF8.GetString(response));
    }

    [TestMethod]
    public Task Execute_Success_With_QoS_0()
    {
        return Execute_Success(MqttQualityOfServiceLevel.AtMostOnce, MqttProtocolVersion.V311);
    }

    [TestMethod]
    public Task Execute_Success_With_QoS_0_MQTT_V5()
    {
        return Execute_Success(MqttQualityOfServiceLevel.AtMostOnce, MqttProtocolVersion.V500);
    }

    [TestMethod]
    public Task Execute_Success_With_QoS_0_MQTT_V5_Use_ResponseTopic()
    {
        return Execute_Success_MQTT_V5(MqttQualityOfServiceLevel.AtMostOnce);
    }

    [TestMethod]
    public Task Execute_Success_With_QoS_1()
    {
        return Execute_Success(MqttQualityOfServiceLevel.AtLeastOnce, MqttProtocolVersion.V311);
    }

    [TestMethod]
    public Task Execute_Success_With_QoS_1_MQTT_V5()
    {
        return Execute_Success(MqttQualityOfServiceLevel.AtLeastOnce, MqttProtocolVersion.V500);
    }

    [TestMethod]
    public Task Execute_Success_With_QoS_1_MQTT_V5_Use_ResponseTopic()
    {
        return Execute_Success_MQTT_V5(MqttQualityOfServiceLevel.AtLeastOnce);
    }

    [TestMethod]
    public Task Execute_Success_With_QoS_2()
    {
        return Execute_Success(MqttQualityOfServiceLevel.ExactlyOnce, MqttProtocolVersion.V311);
    }

    [TestMethod]
    public Task Execute_Success_With_QoS_2_MQTT_V5()
    {
        return Execute_Success(MqttQualityOfServiceLevel.ExactlyOnce, MqttProtocolVersion.V500);
    }

    [TestMethod]
    public Task Execute_Success_With_QoS_2_MQTT_V5_Use_ResponseTopic()
    {
        return Execute_Success_MQTT_V5(MqttQualityOfServiceLevel.ExactlyOnce);
    }


    [TestMethod]
    public Task Execute_Timeout()
    {
        return Assert.ThrowsExactlyAsync<MqttCommunicationTimedOutException>(async () =>
        {
            using var testEnvironment = CreateTestEnvironment();
            await testEnvironment.StartServer();

            var requestSender = await testEnvironment.ConnectClient();

            var rpcClient = new MqttRpcClient(requestSender, new MqttRpcClientOptionsBuilder().Build());
            await rpcClient.ExecuteAsync(TimeSpan.FromSeconds(2), "ping", "", MqttQualityOfServiceLevel.AtMostOnce);
        });
    }

    [TestMethod]
    public Task Execute_Timeout_MQTT_V5_Mixed_Clients()
    {
        return Assert.ThrowsExactlyAsync<MqttCommunicationTimedOutException>(async () =>
        {
            using var testEnvironment = new TestEnvironment(TestContext);
            await testEnvironment.StartServer();
            var responseSender = await testEnvironment.ConnectClient();
            await responseSender.SubscribeAsync("MQTTnet.RPC/+/ping");

            responseSender.ApplicationMessageReceivedAsync += e =>
            {
                Assert.IsNull(e.ApplicationMessage.ResponseTopic);
                return CompletedTask.Instance;
            };

            var requestSender = await testEnvironment.ConnectClient(new MqttClientOptionsBuilder().WithProtocolVersion(MqttProtocolVersion.V500));

            using var rpcClient = new MqttRpcClient(requestSender, new MqttRpcClientOptionsBuilder().Build());
            await rpcClient.ExecuteAsync(TimeSpan.FromSeconds(2), "ping", "", MqttQualityOfServiceLevel.AtMostOnce);
        });
    }

    [TestMethod]
    public Task Execute_With_Custom_Topic_Names()
    {
        return Assert.ThrowsExactlyAsync<MqttCommunicationTimedOutException>(async () =>
        {
            using var testEnvironment = CreateTestEnvironment();
            await testEnvironment.StartServer();

            var rpcClient = await testEnvironment.ConnectRpcClient(new MqttRpcClientOptionsBuilder().WithTopicGenerationStrategy(new TestTopicStrategy()).Build());

            await rpcClient.ExecuteAsync(TimeSpan.FromSeconds(2), "ping", "", MqttQualityOfServiceLevel.AtMostOnce);
        });
    }

    [TestMethod]
    public void Use_Factory()
    {
        var factory = new MqttClientFactory();
        using var client = factory.CreateMqttClient();
        var rpcClient = factory.CreateMqttRpcClient(client);

        Assert.IsNotNull(rpcClient);
    }

    async Task Execute_Success(MqttQualityOfServiceLevel qosLevel, MqttProtocolVersion protocolVersion)
    {
        using var testEnvironment = CreateTestEnvironment();
        await testEnvironment.StartServer();
        var responseSender = await testEnvironment.ConnectClient(new MqttClientOptionsBuilder().WithProtocolVersion(protocolVersion));
        await responseSender.SubscribeAsync("MQTTnet.RPC/+/ping", qosLevel);

        responseSender.ApplicationMessageReceivedAsync += e => responseSender.PublishStringAsync(e.ApplicationMessage.Topic + "/response", "pong");

        var requestSender = await testEnvironment.ConnectClient();

        using var rpcClient = new MqttRpcClient(requestSender, new MqttRpcClientOptionsBuilder().Build());
        var response = await rpcClient.ExecuteAsync(TimeSpan.FromSeconds(5), "ping", "", qosLevel);

        Assert.AreEqual("pong", Encoding.UTF8.GetString(response));
    }

    async Task Execute_Success_MQTT_V5(MqttQualityOfServiceLevel qosLevel)
    {
        using var testEnvironment = CreateTestEnvironment();
        await testEnvironment.StartServer();
        var responseSender = await testEnvironment.ConnectClient(new MqttClientOptionsBuilder().WithProtocolVersion(MqttProtocolVersion.V500));
        await responseSender.SubscribeAsync("MQTTnet.RPC/+/ping", qosLevel);

        responseSender.ApplicationMessageReceivedAsync += async e =>
        {
            await responseSender.PublishStringAsync(e.ApplicationMessage.ResponseTopic, "pong").ConfigureAwait(false);
        };

        var requestSender = await testEnvironment.ConnectClient(new MqttClientOptionsBuilder().WithProtocolVersion(MqttProtocolVersion.V500));

        using var rpcClient = new MqttRpcClient(requestSender, new MqttRpcClientOptionsBuilder().Build());
        var response = await rpcClient.ExecuteAsync(TimeSpan.FromSeconds(5), "ping", "", qosLevel);

        Assert.AreEqual("pong", Encoding.UTF8.GetString(response));
    }

    sealed class TestTopicStrategy : IMqttRpcClientTopicGenerationStrategy
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

    sealed class TestParametersTopicGenerationStrategy : IMqttRpcClientTopicGenerationStrategy
    {
        internal const string ExpectedParamName = "test_param_name";

        public MqttRpcTopicPair CreateRpcTopics(TopicGenerationContext context)
        {
            if (context.Parameters == null)
            {
                throw new InvalidOperationException("Parameters dictionary expected to be not null");
            }

            if (!context.Parameters.TryGetValue(ExpectedParamName, out var paramValue))
            {
                throw new InvalidOperationException($"Parameter with name {ExpectedParamName} not present");
            }

            var requestTopic = $"MQTTnet.RPC/{Guid.NewGuid():N}/{context.MethodName}/{paramValue}";
            var responseTopic = requestTopic + "/response";

            return new MqttRpcTopicPair
            {
                RequestTopic = requestTopic,
                ResponseTopic = responseTopic
            };
        }
    }
}