using System;
using MQTTnet.Core.Client;

namespace MQTTnet.Core.ManagedClient
{
    public interface IManagedMqttClientOptions
    {
        IMqttClientOptions ClientOptions { get; }

        TimeSpan AutoReconnectDelay { get; }

        IManagedMqttClientStorage Storage { get; }
    }
}