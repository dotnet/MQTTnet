using System.Threading.Tasks;

namespace MQTTnet.Client
{
    public interface IMqttClientDisconnectedHandler
    {
        Task HandleDisconnectedAsync(MqttClientDisconnectedEventArgs eventArgs);
    }
}
