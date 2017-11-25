namespace MQTTnet.Client
{
    public class MqttClientCredentials : IMqttClientCredentials
    {
        public string Username { get; set; }

        public string Password { get; set; }
    }
}
