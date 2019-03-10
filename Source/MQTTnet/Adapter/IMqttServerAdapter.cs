using System;
using System.Threading.Tasks;
using MQTTnet.Server;

namespace MQTTnet.Adapter
{
    public interface IMqttServerAdapter : IDisposable
    {
        Action<MqttServerAdapterClientAcceptedEventArgs> ClientAcceptedHandler { get; set; }

        Task StartAsync(IMqttServerOptions options);
        Task StopAsync();
    }
}
