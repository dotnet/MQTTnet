using System;
using System.Collections.Generic;
using MQTTnet.Core.Adapter;

namespace MQTTnet.Core.Server
{
    public interface IMqttServer
    {
        event EventHandler<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived;
        event EventHandler<MqttClientConnectedEventArgs> ClientConnected;

        IList<ConnectedMqttClient> GetConnectedClients();
        void InjectClient(string identifier, IMqttCommunicationAdapter adapter);
        void Publish(MqttApplicationMessage applicationMessage);
        void Start();
        void Stop();
    }
}