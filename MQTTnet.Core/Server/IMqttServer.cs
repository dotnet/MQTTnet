using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MQTTnet.Core.Server
{
    public interface IMqttServer
    {
        event EventHandler<MqttClientConnectedEventArgs> ClientConnected;
        event EventHandler<MqttClientDisconnectedEventArgs> ClientDisconnected;

        event EventHandler<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived;

        IList<ConnectedMqttClient> GetConnectedClients();
        void Publish(MqttApplicationMessage applicationMessage);

        Task StartAsync();
        Task StopAsync();
    }
}