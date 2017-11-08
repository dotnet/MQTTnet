using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MQTTnet.Core.Server
{
    public interface IMqttServer : IApplicationMessageReceiver, IApplicationMessagePublisher
    {
        event EventHandler<MqttClientConnectedEventArgs> ClientConnected;
        event EventHandler<MqttClientDisconnectedEventArgs> ClientDisconnected;

        IList<ConnectedMqttClient> GetConnectedClients();
        void Publish(IEnumerable<MqttApplicationMessage> applicationMessages);

        Task StartAsync();
        Task StopAsync();
    }
}