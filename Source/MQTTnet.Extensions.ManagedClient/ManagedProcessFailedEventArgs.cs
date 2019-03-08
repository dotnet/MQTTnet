using System;

namespace MQTTnet.Extensions.ManagedClient
{
    public class ManagedProcessFailedEventArgs : EventArgs
    {
        public ManagedProcessFailedEventArgs(Exception exception)
        {
            Exception = exception;
        }

        public Exception Exception { get; }
    }
}
