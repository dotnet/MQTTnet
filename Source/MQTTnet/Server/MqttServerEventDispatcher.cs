using System.Threading.Tasks;
using MQTTnet.Client.Receiving;

namespace MQTTnet.Server
{
    public class MqttServerEventDispatcher
    {
        public IMqttServerClientConnectedHandler ClientConnectedHandler { get; set; }

        public IMqttServerClientDisconnectedHandler ClientDisconnectedHandler { get; set; }

        public IMqttServerClientSubscribedTopicHandler ClientSubscribedTopicHandler { get; set; }

        public IMqttServerClientUnsubscribedTopicHandler ClientUnsubscribedTopicHandler { get; set; }

        public IMqttApplicationMessageHandler ApplicationMessageReceivedHandler { get; set; }

        public Task HandleClientConnectedAsync(string clientId)
        {
            var handler = ClientConnectedHandler;
            if (handler == null)
            {
                return Task.FromResult(0);
            }

            return handler.HandleClientConnectedAsync(new MqttServerClientConnectedEventArgs(clientId));
        }

        public Task HandleClientDisconnectedAsync(string clientId, MqttClientDisconnectType disconnectType)
        {
            var handler = ClientDisconnectedHandler;
            if (handler == null)
            {
                return Task.FromResult(0);
            }

            return handler.HandleClientDisconnectedAsync(new MqttServerClientDisconnectedEventArgs(clientId, disconnectType));
        }

        public Task HandleClientSubscribedTopicAsync(string clientId, TopicFilter topicFilter)
        {
            var handler = ClientSubscribedTopicHandler;
            if (handler == null)
            {
                return Task.FromResult(0);
            }

            return handler.HandleClientSubscribedTopicAsync(new MqttServerClientSubscribedTopicEventArgs(clientId, topicFilter));
        }

        public Task HandleClientUnsubscribedTopicAsync(string clientId, string topicFilter)
        {
            var handler = ClientUnsubscribedTopicHandler;
            if (handler == null)
            {
                return Task.FromResult(0);
            }

            return handler.HandleClientUnsubscribedTopicAsync(new MqttServerClientUnsubscribedTopicEventArgs(clientId, topicFilter));
        }

        public Task HandleApplicationMessageReceivedAsync(string senderClientId, MqttApplicationMessage applicationMessage)
        {
            var handler = ApplicationMessageReceivedHandler;
            if (handler == null)
            {
                return Task.FromResult(0);
            }

            return handler.HandleApplicationMessageAsync(new MqttApplicationMessageReceivedEventArgs(senderClientId, applicationMessage));
        }
    }
}
