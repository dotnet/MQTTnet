using System.Threading.Tasks;

namespace MQTTnet.Server
{
    public interface IMqttServerClientDisconnectedHandler
    {
        Task HandleClientDisconnectedAsync(MqttServerClientDisconnectedEventArgs eventArgs);
    }
}
