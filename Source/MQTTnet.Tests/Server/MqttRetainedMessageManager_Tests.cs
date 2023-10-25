using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTTnet.Tests.Server
{
    [TestClass]
    public sealed class MqttRetainedMessageManager_Tests
    {
        [TestMethod]
        public async Task MqttRetainedMessageManager_GetUndefinedTopic()
        {
            var logger = new Mockups.TestLogger();
            var eventContainer = new MQTTnet.Server.MqttServerEventContainer();
            var retainedMessagesManager = new MQTTnet.Server.MqttRetainedMessagesManager(eventContainer, logger);
            var task = retainedMessagesManager.GetMessage("undefined");
            Assert.IsNotNull(task, "Task should not be null");
            var result = await task;
            Assert.IsNull(result, "Null result expected");
        }
    }
}
