using System;
using System.Collections.Generic;
using MQTTnet.Packets;

namespace MQTTnet.Server
{
    public interface IMqttServerPersistedSession
    {
        /// <summary>
        /// Gets the client identifier.
        /// Hint: This identifier needs to be unique over all used clients / devices on the broker to avoid connection issues.
        /// </summary>
        string ClientId { get; }

        IDictionary<object, object> Items { get; }

        IList<MqttTopicFilter> Subscriptions { get; }

        /// <summary>
        /// Gets the last will message.
        /// In MQTT, you use the last will message feature to notify other clients about an ungracefully disconnected client.
        /// </summary>
        MqttApplicationMessage WillMessage { get; }

        /// <summary>
        /// Gets the will delay interval.
        /// This is the time between the client disconnect and the time the will message will be sent.
        /// </summary>
        uint? WillDelayInterval { get; }

        DateTime? SessionExpiryTimestamp { get; }

        IList<MqttQueuedApplicationMessage> PendingApplicationMessages { get; }
    }
}
