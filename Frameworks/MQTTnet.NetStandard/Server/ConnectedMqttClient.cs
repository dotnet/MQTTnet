using MQTTnet.Serializer;

namespace MQTTnet.Server
{
    public class ConnectedMqttClient
    {
        public string ClientId { get; set; }

        public MqttProtocolVersion ProtocolVersion { get; set; }
    }
}
