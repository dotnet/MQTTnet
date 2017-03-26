using System;

namespace MQTTnet.Core.Exceptions
{
    public class MqttCommunicationException : Exception
    {
        public MqttCommunicationException()
        {
        }

        public MqttCommunicationException(Exception innerException)
            : base(innerException.Message, innerException)
        {
        }

        public MqttCommunicationException(string message)
            : base(message)
        {
        }
    }
}
