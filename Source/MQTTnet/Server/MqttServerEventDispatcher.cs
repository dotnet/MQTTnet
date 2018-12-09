using System;

namespace MQTTnet.Server
{
    public class MqttServerEventDispatcher
    {
        public event EventHandler<MqttClientSubscribedTopicEventArgs> ClientSubscribedTopic;

        public event EventHandler<MqttClientUnsubscribedTopicEventArgs> ClientUnsubscribedTopic;

        public event EventHandler<MqttClientConnectedEventArgs> ClientConnected;

        public event EventHandler<MqttClientDisconnectedEventArgs> ClientDisconnected;

        public event EventHandler<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived;

        public void OnClientSubscribedTopic(string clientId, TopicFilter topicFilter)
        {
            ClientSubscribedTopic?.Invoke(this, new MqttClientSubscribedTopicEventArgs(clientId, topicFilter));
        }

        public void OnClientUnsubscribedTopic(string clientId, string topicFilter)
        {
            ClientUnsubscribedTopic?.Invoke(this, new MqttClientUnsubscribedTopicEventArgs(clientId, topicFilter));
        }

        public void OnClientDisconnected(string clientId, bool wasCleanDisconnect)
        {
            ClientDisconnected?.Invoke(this, new MqttClientDisconnectedEventArgs(clientId, wasCleanDisconnect));
        }

        public void OnApplicationMessageReceived(string senderClientId, MqttApplicationMessage applicationMessage)
        {
            ApplicationMessageReceived?.Invoke(this, new MqttApplicationMessageReceivedEventArgs(senderClientId, applicationMessage));
        }

        public void OnClientConnected(string clientId)
        {
            ClientConnected?.Invoke(this, new MqttClientConnectedEventArgs(clientId));
        }
    }
}
