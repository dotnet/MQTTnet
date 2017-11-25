using System;

namespace MQTTnet.Exceptions
{
    public sealed class MqttProtocolViolationException : Exception
    {
        public MqttProtocolViolationException(string message)
            : base(message)
        {
        }
    }
}
