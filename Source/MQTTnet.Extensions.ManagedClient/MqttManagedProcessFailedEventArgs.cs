using System;

namespace MQTTnet.Extensions.ManagedClient
{
    public class MqttManagedProcessFailedEventArgs : EventArgs
    {
        public MqttManagedProcessFailedEventArgs(Exception exception)
        {
            Exception = exception;
        }

        public Exception Exception { get; }
    }
}
