using System.Collections.Generic;
using MQTTnet.Packets;

namespace MQTTnet.Client
{
    public sealed class MqttClientSubscribeResult
    {
        public List<MqttClientSubscribeResultItem> Items { get; } = new List<MqttClientSubscribeResultItem>();
        
        /// <summary>
        /// Gets the user properties which were part of the SUBACK packet.
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