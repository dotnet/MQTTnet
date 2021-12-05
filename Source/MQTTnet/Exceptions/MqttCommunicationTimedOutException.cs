using System;

namespace MQTTnet.Exceptions
{
    public class MqttCommunicationTimedOutException : MqttCommunicationException
    {
        public MqttCommunicationTimedOutException()
        {
        }

        public MqttCommunicationTimedOutException(Exception innerException) : base("The operation has timed out.", innerException)
        {
        }
    }
}
