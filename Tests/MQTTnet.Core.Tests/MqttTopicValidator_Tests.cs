using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Exceptions;
using MQTTnet.Protocol;

namespace MQTTnet.Tests
{
    [TestClass]
    public class MqttTopicValidator_Tests
    {
        [TestMethod]
        public void Valid_Topic()
        {
            MqttTopicValidator.ThrowIfInvalid("/a/b/c");
        }

        [TestMethod]
        [ExpectedException(typeof(MqttProtocolViolationException))]
        public void Invalid_Topic_Plus()
        {
            MqttTopicValidator.ThrowIfInvalid("/a/+/c");
        }

        [TestMethod]
        [ExpectedException(typeof(MqttProtocolViolationException))]
        public void Invalid_Topic_Hash()
        {
            MqttTopicValidator.ThrowIfInvalid("/a/#/c");
        }

        [TestMethod]
        [ExpectedException(typeof(MqttProtocolViolationException))]
        public void Invalid_Topic_Empty()
        {
            MqttTopicValidator.ThrowIfInvalid(string.Empty);
        }
    }
}
