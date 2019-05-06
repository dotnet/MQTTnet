using System.Threading.Tasks;

namespace MQTTnet.Server
{
    public interface IMqttServerClientSubscribedTopicHandler
    {
        Task HandleClientSubscribedTopicAsync(MqttServerClientSubscribedTopicEventArgs eventArgs);
    }
}
