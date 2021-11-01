using System;
using System.Collections.Generic;

namespace MQTTnet.Server
{
    public sealed class RetainedMessageChangedEventArgs : EventArgs
    {
        public string ClientId { get; internal set; }
        
        public MqttApplicationMessage ChangedRetainedMessage { get; internal set; }
        
        public List<MqttApplicationMessage> StoredRetainedMessages { get; internal set; }
    }
}