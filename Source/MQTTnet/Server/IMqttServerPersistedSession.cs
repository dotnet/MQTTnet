using System;
using System.Collections.Generic;

namespace MQTTnet.Server
{
    public interface IMqttServerPersistedSession
    {
        string ClientId { get; }

        IDictionary<object, object> Items { get; }

        IList<MqttTopicFilter> Subscriptions { get; }

        MqttApplicationMessage WillMessage { get; }

        uint? WillDelayInterval { get; }

        DateTime? SessionExpiryTimestamp { get; }

        IList<MqttQueuedApplicationMessage> PendingApplicationMessages { get; }
    }
}
