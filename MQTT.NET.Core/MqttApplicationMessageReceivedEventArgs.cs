using System;

namespace MQTTnet.Core
{
    public class MqttApplicationMessageReceivedEventArgs : EventArgs
    {
        public MqttApplicationMessageReceivedEventArgs(MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));

            ApplicationMessage = applicationMessage;
        }

        public MqttApplicationMessage ApplicationMessage { get; }
    }
}
