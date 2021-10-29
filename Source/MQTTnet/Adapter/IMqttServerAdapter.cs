using System;
using System.Threading.Tasks;
using MQTTnet.Diagnostics;
using MQTTnet.Server;

namespace MQTTnet.Adapter
{
    public interface IMqttServerAdapter : IDisposable
    {
        Func<IMqttChannelAdapter, Task> ClientHandler { get; set; }

        Task StartAsync(MqttServerOptions options, IMqttNetLogger logger);
        Task StopAsync();
    }
}
