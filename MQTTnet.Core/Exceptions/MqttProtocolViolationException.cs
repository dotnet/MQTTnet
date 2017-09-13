using System;

namespace MQTTnet.Core.Exceptions
{
    public sealed class MqttProtocolViolationException : Exception
    {
        public MqttProtocolViolationException(string message)
            : base(message)
        {
        }
    }
}
