using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MQTTnet.Core.Adapter;

namespace MQTTnet.Core.Server
{
    public interface IMqttServer
    {
        event EventHandler<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived;
        event EventHandler<MqttClientConnectedEventArgs> ClientConnected;
        event EventHandler<MqttClientDisconnectedEventArgs> ClientDisconnected;

        IReadOnlyList<ConnectedMqttClient> GetConnectedClients();
        void Publish(MqttApplicationMessage applicationMessage);

        Task StartAsync();
        Task StopAsync();
    }
}