using System;
using System.Collections.Generic;

namespace MQTTnet.Server
{
    public sealed class LoadingRetainedMessagesEventArgs : EventArgs
    {
        public List<MqttApplicationMessage> StoredRetainedMessages { get; set; } = new List<MqttApplicationMessage>();
    }
}