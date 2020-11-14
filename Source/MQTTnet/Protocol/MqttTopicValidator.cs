using System;
using MQTTnet.Exceptions;

namespace MQTTnet.Protocol
{
    public static class MqttTopicValidator
    {
        public static void ThrowIfInvalid(MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));

            if (!applicationMessage.TopicAlias.HasValue)
            {
                ThrowIfInvalid(applicationMessage.Topic);
            }
            else
            {
                if (applicationMessage.TopicAlias.Value == 0)
                {
                    throw new MqttProtocolViolationException("The topic alias cannot be 0.");
                }
            }
        }

        public static void ThrowIfInvalid(string topic)
        {
            if (string.IsNullOrEmpty(topic))
            {
                throw new MqttProtocolViolationException("Topic should not be empty.");
            }

            foreach(var @char in topic)
            {
                if (@char == '+')
                {
                    throw new MqttProtocolViolationException("The character '+' is not allowed in topics.");
                }

                if (@char == '#')
                {
                    throw new MqttProtocolViolationException("The character '#' is not allowed in topics.");
                }
            }
        }
    }
}
