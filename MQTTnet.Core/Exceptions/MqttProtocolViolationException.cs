using System;

namespace MQTTnet.Core.Exceptions
{
    public class MqttProtocolViolationException : Exception
    {
        public MqttProtocolViolationException(string message) 
            : base(message)
        {
        }
    }
}
