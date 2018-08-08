using System;
using MQTTnet.Client;

namespace MQTTnet.Extensions.ManagedClient
{
    public class ManagedMqttClientOptions : IManagedMqttClientOptions
    {
        public IMqttClientOptions ClientOptions { get; set; }

        public TimeSpan AutoReconnectDelay { get; set; } = TimeSpan.FromSeconds(5);

        public TimeSpan ConnectionCheckInterval { get; set; } = TimeSpan.FromSeconds(1);

        public IManagedMqttClientStorage Storage { get; set; }
    }
}
