using System.Threading.Tasks;

namespace MQTTnet.Client.Receiving
{
    public interface IMqttApplicationMessageReceivedHandler
    {
        ValueTask HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs);
    }
}
