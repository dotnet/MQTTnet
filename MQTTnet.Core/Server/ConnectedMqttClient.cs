using MQTTnet.Core.Serializer;

namespace MQTTnet.Core.Server
{
    public class ConnectedMqttClient
    {
        public string ClientId { get; set; }

        public MqttProtocolVersion ProtocolVersion { get; set; }
    }
}
