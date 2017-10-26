using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MQTTnet.Core.Client;

namespace MQTTnet.Core.Server
{
    public interface IMqttServer : IApplicationMessageReceiver, IApplicationMessagePublisher
    {
        event EventHandler<MqttClientConnectedEventArgs> ClientConnected;
        event EventHandler<MqttClientDisconnectedEventArgs> ClientDisconnected;

        IList<ConnectedMqttClient> GetConnectedClients();

        Task StartAsync();
        Task StopAsync();
    }
}