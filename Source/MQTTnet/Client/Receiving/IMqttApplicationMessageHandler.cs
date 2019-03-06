using System.Threading.Tasks;

namespace MQTTnet.Client.Receiving
{
    public interface IMqttApplicationMessageHandler
    {
        Task HandleApplicationMessageAsync(MqttApplicationMessageReceivedEventArgs eventArgs);
    }
}
