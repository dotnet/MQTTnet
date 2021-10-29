using System.Collections.Generic;
using MQTTnet.Packets;

namespace MQTTnet.Client
{
    public sealed class MqttClientUnsubscribeResult
    {
        public List<MqttClientUnsubscribeResultItem> Items { get; }  =new List<MqttClientUnsubscribeResultItem>();

        /// <summary>
        /// Gets the user properties which were part of the UNSUBACK packet.
        /// MQTTv5 only.
        /// </summary>
        public List<MqttUserProperty> UserProperties { get; } = new List<MqttUserProperty>();
        
        /// <summary>
        /// Gets the reason string.
        /// MQTTv5 only.
        /// </summary>
        public string ReasonString { get; internal set; }
    }
}
