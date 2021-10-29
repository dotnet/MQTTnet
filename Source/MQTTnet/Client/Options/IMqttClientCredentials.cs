namespace MQTTnet.Client
{
    public interface IMqttClientCredentials
    {
        string Username { get; }
        byte[] Password { get; }
    }
}