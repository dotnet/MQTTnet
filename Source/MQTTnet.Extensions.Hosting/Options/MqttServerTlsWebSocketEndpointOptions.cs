namespace MQTTnet.Extensions.Hosting.Options
{
    public class MqttServerTlsWebSocketEndpointOptions : MqttServerWebSocketEndpointBaseOptions
    {

        public MqttServerTlsWebSocketEndpointOptions()
        {
            Port = 443;
        }

    }
}
