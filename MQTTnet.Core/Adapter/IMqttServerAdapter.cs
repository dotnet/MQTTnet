using System;
using System.Threading.Tasks;
using MQTTnet.Core.Server;

namespace MQTTnet.Core.Adapter
{
    public interface IMqttServerAdapter
    {
        event EventHandler<MqttClientConnectedEventArgs> ClientConnected;

        Task StartAsync(MqttServerOptions options);
        Task StopAsync();
    }
}
