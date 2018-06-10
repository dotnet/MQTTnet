using System;
using System.Threading.Tasks;
using MQTTnet.Server;

namespace MQTTnet.Adapter
{
    public interface IMqttServerAdapter : IDisposable
    {
        event EventHandler<MqttServerAdapterClientAcceptedEventArgs> ClientAccepted;

        Task StartAsync(IMqttServerOptions options);
        Task StopAsync();
    }
}
