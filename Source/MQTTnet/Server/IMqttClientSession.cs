using System.Threading.Tasks;

namespace MQTTnet.Server
{
    public interface IMqttClientSession
    {
        string ClientId { get; }

        Task StopAsync();
    }
}