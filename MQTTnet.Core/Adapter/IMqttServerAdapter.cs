using System;
using System.Threading.Tasks;
using MQTTnet.Core.Server;

namespace MQTTnet.Core.Adapter
{
    public interface IMqttServerAdapter
    {
        event EventHandler<MqttServerAdapterClientAcceptedEventArgs> ClientAccepted;

        Task StartAsync(MqttServerOptions options);
        Task StopAsync();
    }
}
