using System;
using MQTTnet.Client;

namespace MQTTnet.ManagedClient
{
    public interface IManagedMqttClientOptions
    {
        IMqttClientOptions ClientOptions { get; }

        TimeSpan AutoReconnectDelay { get; }

        IManagedMqttClientStorage Storage { get; }
    }
}