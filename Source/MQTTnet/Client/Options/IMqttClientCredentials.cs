namespace MQTTnet.Client.Options
{
    public interface IMqttClientCredentials
    {
        string Username { get; }
        byte[] Password { get; }
    }
}