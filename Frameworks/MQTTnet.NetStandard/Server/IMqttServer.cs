using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MQTTnet.Server
{
    public interface IMqttServer : IApplicationMessageReceiver, IApplicationMessagePublisher
    {
        event EventHandler<MqttClientConnectedEventArgs> ClientConnected;
        event EventHandler<MqttClientDisconnectedEventArgs> ClientDisconnected;
        event EventHandler<MqttServerStartedEventArgs> Started;

        Task<IList<ConnectedMqttClient>> GetConnectedClientsAsync();

        Task StartAsync(MqttServerOptions options);
        Task StopAsync();
    }
}