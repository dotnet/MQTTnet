using System.Threading.Tasks;

namespace MQTTnet.Client.Disconnecting
{
    public interface IMqttClientDisconnectedHandler
    {
        Task HandleDisconnectedAsync(MqttClientDisconnectedEventArgs eventArgs);
    }
}
