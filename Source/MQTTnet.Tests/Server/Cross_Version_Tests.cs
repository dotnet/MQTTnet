using System.Text;
using MQTTnet.Formatter;

namespace MQTTnet.Tests.Server;

// ReSharper disable InconsistentNaming
[TestClass]
public sealed class Cross_Version_Tests : BaseTestClass
{
    [TestMethod]
    public async Task Send_V311_Receive_V500()
    {
        using var testEnvironment = CreateTestEnvironment();
        await testEnvironment.StartServer();

        var receiver = await testEnvironment.ConnectClient(o => o.WithProtocolVersion(MqttProtocolVersion.V500));
        var receivedApplicationMessages = testEnvironment.CreateApplicationMessageHandler(receiver);
        await receiver.SubscribeAsync("#");

        var sender = await testEnvironment.ConnectClient();

        var applicationMessage = new MqttApplicationMessageBuilder().WithTopic("My/Message").WithPayload("My_Payload").Build();
        await sender.PublishAsync(applicationMessage);

        await LongTestDelay();

        Assert.HasCount(1, receivedApplicationMessages.ReceivedEventArgs);
        Assert.AreEqual("My/Message", receivedApplicationMessages.ReceivedEventArgs[0].ApplicationMessage.Topic);
        Assert.AreEqual("My_Payload", receivedApplicationMessages.ReceivedEventArgs[0].ApplicationMessage.ConvertPayloadToString());
    }

    [TestMethod]
    public async Task Send_V500_Receive_V311()
    {
        using var testEnvironment = CreateTestEnvironment(MqttProtocolVersion.V500);
        await testEnvironment.StartServer();

        var receiver = await testEnvironment.ConnectClient(o => o.WithProtocolVersion(MqttProtocolVersion.V311));
        var receivedApplicationMessages = testEnvironment.CreateApplicationMessageHandler(receiver);
        await receiver.SubscribeAsync("#");

        var sender = await testEnvironment.ConnectClient();

        var applicationMessage = new MqttApplicationMessageBuilder().WithTopic("My/Message")
            .WithPayload("My_Payload")
            .WithUserProperty("A", Encoding.UTF8.GetBytes("B"))
            .WithResponseTopic("Response")
            .WithCorrelationData(Encoding.UTF8.GetBytes("Correlation"))
            .Build();

        await sender.PublishAsync(applicationMessage);

        await LongTestDelay();

        Assert.HasCount(1, receivedApplicationMessages.ReceivedEventArgs);
        Assert.AreEqual("My/Message", receivedApplicationMessages.ReceivedEventArgs[0].ApplicationMessage.Topic);
        Assert.AreEqual("My_Payload", receivedApplicationMessages.ReceivedEventArgs[0].ApplicationMessage.ConvertPayloadToString());
    }
}