using MQTTnet.Exceptions;

namespace MQTTnet.Protocol
{
    public static class MqttTopicValidator
    {
        public static void ThrowIfInvalid(string topic)
        {
            if (string.IsNullOrEmpty(topic))
            {
                throw new MqttProtocolViolationException("Topic should not be empty.");
            }

            if (topic.Contains("+"))
            {
                throw new MqttProtocolViolationException("The character '+' is not allowed in topics.");
            }

            if (topic.Contains("#"))
            {
                throw new MqttProtocolViolationException("The character '#' is not allowed in topics.");
            }
        }
    }
}
