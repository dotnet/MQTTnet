using System;

namespace MQTTnet.ManagedClient
{
    public class ApplicationMessageProcessedEventArgs : EventArgs
    {
        public ApplicationMessageProcessedEventArgs(MqttApplicationMessage applicationMessage, Exception exception)
        {
            ApplicationMessage = applicationMessage ?? throw new ArgumentNullException(nameof(applicationMessage));
            Exception = exception;
        }

        public MqttApplicationMessage ApplicationMessage { get; }
        public Exception Exception { get; }

        public bool HasFailed => Exception != null;
        public bool HasSucceeded => Exception == null;
    }
}
