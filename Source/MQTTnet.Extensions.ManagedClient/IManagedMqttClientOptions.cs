using System;
using MQTTnet.Client;

namespace MQTTnet.Extensions.ManagedClient
{
    public interface IManagedMqttClientOptions
    {
        IMqttClientOptions ClientOptions { get; }

        TimeSpan AutoReconnectDelay { get; }

        IManagedMqttClientStorage Storage { get; }
    }
}