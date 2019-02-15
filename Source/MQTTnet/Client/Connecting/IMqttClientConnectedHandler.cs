using System.Threading.Tasks;

namespace MQTTnet.Client.Connecting
{
    public interface IMqttClientConnectedHandler
    {
        Task HandleConnectedAsync(MqttClientConnectedEventArgs eventArgs);
    }
}
