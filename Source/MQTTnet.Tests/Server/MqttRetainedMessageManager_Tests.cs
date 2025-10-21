using MQTTnet.Server.Internal;

namespace MQTTnet.Tests.Server;

// ReSharper disable InconsistentNaming
[TestClass]
public sealed class MqttRetainedMessageManager_Tests
{
    [TestMethod]
    public async Task MqttRetainedMessageManager_GetUndefinedTopic()
    {
        var logger = new Mockups.TestLogger();
        var eventContainer = new MqttServerEventContainer();
        var retainedMessagesManager = new MqttRetainedMessagesManager(eventContainer, logger);
        var task = retainedMessagesManager.GetMessage("undefined");
        Assert.IsNotNull(task, "Task should not be null");
        var result = await task;
        Assert.IsNull(result, "Null result expected");
    }
}