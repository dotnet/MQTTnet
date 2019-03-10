using System.Threading.Tasks;

namespace MQTTnet.Server
{
    public interface IMqttServerClientUnsubscribedTopicHandler
    {
        Task HandleClientUnsubscribedTopicAsync(MqttServerClientUnsubscribedTopicEventArgs eventArgs);
    }
}
