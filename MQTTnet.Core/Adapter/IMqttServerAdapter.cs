using System;
using System.Threading.Tasks;
using MQTTnet.Core.Server;

namespace MQTTnet.Core.Adapter
{
    public interface IMqttServerAdapter
    {
        event Action<IMqttCommunicationAdapter> ClientAccepted;

        Task StartAsync(MqttServerOptions options);
        Task StopAsync();
    }
}
