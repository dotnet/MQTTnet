using System;

namespace MQTTnet.Exceptions
{
    public sealed class MqttCommunicationTimedOutException : MqttCommunicationException
    {
        public MqttCommunicationTimedOutException() : base() { }
        public MqttCommunicationTimedOutException(Exception innerException) : base(innerException) { }

    }
}
