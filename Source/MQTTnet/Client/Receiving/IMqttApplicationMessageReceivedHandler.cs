using System.Threading.Tasks;

namespace MQTTnet.Client.Receiving
{
    public interface IMqttApplicationMessageReceivedHandler
    {
        Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs);
    }
}
