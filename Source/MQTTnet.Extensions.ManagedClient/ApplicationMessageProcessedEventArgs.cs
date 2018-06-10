using System;

namespace MQTTnet.Extensions.ManagedClient
{
    public class ApplicationMessageProcessedEventArgs : EventArgs
    {
        public ApplicationMessageProcessedEventArgs(ManagedMqttApplicationMessage applicationMessage, Exception exception)
        {
            ApplicationMessage = applicationMessage ?? throw new ArgumentNullException(nameof(applicationMessage));
            Exception = exception;
        }

        public ManagedMqttApplicationMessage ApplicationMessage { get; }
        public Exception Exception { get; }

        public bool HasFailed => Exception != null;
        public bool HasSucceeded => Exception == null;
    }
}
