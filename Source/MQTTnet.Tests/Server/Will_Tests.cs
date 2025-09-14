using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Internal;
using MQTTnet.Protocol;

namespace MQTTnet.Tests.Server;

// ReSharper disable InconsistentNaming
[TestClass]
public sealed class Will_Tests : BaseTestClass
{
    [TestMethod]
    public async Task Intercept_Will_Message()
    {
        using var testEnvironment = CreateTestEnvironment();
        var server = await testEnvironment.StartServer().ConfigureAwait(false);

        MqttApplicationMessage willMessage = null;
        server.InterceptingPublishAsync += eventArgs =>
        {
            willMessage = eventArgs.ApplicationMessage;
            return CompletedTask.Instance;
        };

        await testEnvironment.ConnectClient(new MqttClientOptionsBuilder()).ConfigureAwait(false);
        var clientOptions = new MqttClientOptionsBuilder().WithWillTopic("My/last/will").WithWillQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce);
        var takeOverClient = await testEnvironment.ConnectClient(clientOptions).ConfigureAwait(false);
        takeOverClient.Dispose(); // Dispose will not send a DISCONNECT pattern first so the will message must be sent.

        await LongTestDelay().ConfigureAwait(false);

        Assert.IsNotNull(willMessage);
    }

    [TestMethod]
    public async Task Will_Message_Do_Not_Send_On_Clean_Disconnect()
    {
        using var testEnvironment = CreateTestEnvironment();
        await testEnvironment.StartServer();

        var receiver = await testEnvironment.ConnectClient().ConfigureAwait(false);

        var receivedMessages = testEnvironment.CreateApplicationMessageHandler(receiver);

        await receiver.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("#").Build());

        var clientOptions = new MqttClientOptionsBuilder().WithWillTopic("My/last/will");
        var sender = await testEnvironment.ConnectClient(clientOptions).ConfigureAwait(false);
        await sender.DisconnectAsync().ConfigureAwait(false);

        await LongTestDelay().ConfigureAwait(false);

        Assert.AreEqual(0, receivedMessages.ReceivedEventArgs.Count);
    }

    [TestMethod]
    public async Task Will_Message_Send()
    {
        using var testEnvironment = CreateTestEnvironment();
        await testEnvironment.StartServer();

        var receiver = await testEnvironment.ConnectClient(new MqttClientOptionsBuilder()).ConfigureAwait(false);
        var receivedMessages = testEnvironment.CreateApplicationMessageHandler(receiver);
        await receiver.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("#").Build());

        var clientOptions = new MqttClientOptionsBuilder().WithWillTopic("My/last/will").WithWillQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce);
        var takeOverClient = await testEnvironment.ConnectClient(clientOptions).ConfigureAwait(false);
        takeOverClient.Dispose(); // Dispose will not send a DISCONNECT pattern first so the will message must be sent.

        await LongTestDelay().ConfigureAwait(false);

        Assert.AreEqual(1, receivedMessages.ReceivedEventArgs.Count);
    }
}