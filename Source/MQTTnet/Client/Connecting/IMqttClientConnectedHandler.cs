using System.Threading.Tasks;

namespace MQTTnet.Client
{
    public interface IMqttClientConnectedHandler
    {
        Task HandleConnectedAsync(MqttClientConnectedEventArgs eventArgs);
    }
}
