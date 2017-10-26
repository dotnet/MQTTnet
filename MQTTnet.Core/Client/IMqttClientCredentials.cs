namespace MQTTnet.Core.Client
{
    public interface IMqttClientCredentials
    {
        string Password { get; }
        string Username { get; }
    }
}