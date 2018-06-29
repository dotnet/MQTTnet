namespace MQTTnet.Client
{
    public interface IMqttClientCredentials
    {
        string Password { get; }
        string Username { get; }
    }
}