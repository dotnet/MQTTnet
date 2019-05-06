namespace MQTTnet.Client.Options
{
    public interface IMqttClientCredentials
    {
        string Password { get; }
        string Username { get; }
    }
}