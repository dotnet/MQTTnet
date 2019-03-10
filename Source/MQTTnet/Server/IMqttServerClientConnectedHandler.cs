using System.Threading.Tasks;

namespace MQTTnet.Server
{
    public interface IMqttServerClientConnectedHandler
    {
        Task HandleClientConnectedAsync(MqttServerClientConnectedEventArgs eventArgs);
    }
}
