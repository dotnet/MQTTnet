using System;
using MQTTnet.Client;

namespace MQTTnet.Extensions.ManagedClient
{
    public interface IManagedMqttClientOptions
    {
        IMqttClientOptions ClientOptions { get; }

        TimeSpan AutoReconnectDelay { get; }

        TimeSpan ConnectionCheckInterval { get; }

        IManagedMqttClientStorage Storage { get; }
    }
}