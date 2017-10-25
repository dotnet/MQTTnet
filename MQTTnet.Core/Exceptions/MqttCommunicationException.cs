using System;

namespace MQTTnet.Core.Exceptions
{
    public class MqttCommunicationException : Exception
    {
        protected MqttCommunicationException()
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
