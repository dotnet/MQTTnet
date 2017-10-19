
using System;

namespace MQTTnet.Core.Client
{
    public class MqttClientManagedOptions: MqttClientTcpOptions
    {
        public bool UseAutoReconnect { get; set; }
        public TimeSpan AutoReconnectDelay { get; set; }
        public IMqttClientQueuedStorage Storage { get; set; }
    }
}
