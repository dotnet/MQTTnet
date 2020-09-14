using System;
using MQTTnet.Client.Options;
using MQTTnet.Server;

namespace MQTTnet.Extensions.ManagedClient
{
    public interface IManagedMqttClientOptions
    {
        IMqttClientOptions ClientOptions { get; }

        bool AutoReconnect { get; }

        TimeSpan AutoReconnectDelay { get; }

        TimeSpan ConnectionCheckInterval { get; }

        IManagedMqttClientStorage Storage { get; }

        int MaxPendingMessages { get; }

        MqttPendingMessagesOverflowStrategy PendingMessagesOverflowStrategy { get; }
    }
}