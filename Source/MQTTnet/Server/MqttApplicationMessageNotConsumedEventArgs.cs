using System;

namespace MQTTnet.Server
{
    public sealed class MqttApplicationMessageNotConsumedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the application message which was not consumed by any client.
        /// </summary>
        public MqttApplicationMessage ApplicationMessage { get; internal set; }

        /// <summary>
        /// Gets the ID of the client which has sent the affected application message.
        /// </summary>
        public string SenderClientId { get; internal set; }
    }
}