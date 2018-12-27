using System;
using System.Threading.Tasks;

namespace MQTTnet.Server
{
    public interface IMqttClientSession : IDisposable
    {
        string ClientId { get; }

        Task StopAsync(MqttClientDisconnectType disconnectType);
    }
}