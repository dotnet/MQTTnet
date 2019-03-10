using System;

namespace MQTTnet.Extensions.ManagedClient
{
    public class ManagedProcessFailedEventArgs : EventArgs
    {
        public ManagedProcessFailedEventArgs(Exception exception)
        {
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        }

        public Exception Exception { get; }
    }
}