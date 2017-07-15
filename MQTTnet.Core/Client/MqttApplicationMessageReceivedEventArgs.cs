using System;

namespace MQTTnet.Core.Client
{
    public sealed class MqttApplicationMessageReceivedEventArgs : EventArgs
    {
        public MqttApplicationMessageReceivedEventArgs(MqttApplicationMessage applicationMessage)
        {
            ApplicationMessage = applicationMessage ?? throw new ArgumentNullException(nameof(applicationMessage));
        }

        public MqttApplicationMessage ApplicationMessage { get; }
    }
}
