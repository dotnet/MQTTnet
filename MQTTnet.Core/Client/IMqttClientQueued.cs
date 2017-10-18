using System.Threading.Tasks;

namespace MQTTnet.Core.Client
{
    public interface IMqttClientQueued: IMqttClient
    {
        Task ConnectAsync(MqttClientQueuedOptions options);
    }
}
