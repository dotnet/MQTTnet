using System;
using MQTTnet.Client;

namespace MQTTnet.ManagedClient
{
    public class ManagedMqttClientOptions : IManagedMqttClientOptions
    {
        public IMqttClientOptions ClientOptions { get; set; }

        public TimeSpan AutoReconnectDelay { get; set; } = TimeSpan.FromSeconds(5);

        public IManagedMqttClientStorage Storage { get; set; }
    }
}
