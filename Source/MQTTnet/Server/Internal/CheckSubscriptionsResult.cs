using System.Collections.Generic;
using MQTTnet.Protocol;

namespace MQTTnet.Server.Internal
{
    public struct CheckSubscriptionsResult
    {
        public static CheckSubscriptionsResult NotSubscribed = new CheckSubscriptionsResult();

        public bool IsSubscribed { get; set; }

        public bool RetainAsPublished { get; set; }
        
        public List<uint> SubscriptionIdentifiers { get; set; }
        
        /// <summary>
        /// Gets or sets the quality of service level.
        /// The Quality of Service (QoS) level is an agreement between the sender of a message and the receiver of a message that defines the guarantee of delivery for a specific message.
        /// There are 3 QoS levels in MQTT:
        /// - At most once  (0): Message gets delivered no time, once or multiple times.
        /// - At least once (1): Message gets delivered at least once (one time or more often).
        /// - Exactly once  (2): Message gets delivered exactly once (It's ensured that the message only comes once).
        /// </summary>
        public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; }
    }
}
