namespace MQTTnet.Server.Configuration
{
    public class EndpointConfiguration
    {
        public int Port { get; set; } = 1883;

        public int BacklogSize { get; set; } = 10;
    }
}
