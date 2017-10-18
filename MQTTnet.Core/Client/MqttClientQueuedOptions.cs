

namespace MQTTnet.Core.Client
{
    public class MqttClientQueuedOptions: MqttClientTcpOptions
    {
        public bool UsePersistence { get; set; }

        public IMqttClientQueuedStorage Storage { get; set; }
    }
}
