using System;

namespace MQTTnet.Exceptions
{
    public class MqttProtocolViolationException : Exception
    {
        public MqttProtocolViolationException(string message)
            : base(message)
        {
        }
    }
}
