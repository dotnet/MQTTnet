using System.Threading.Tasks;

namespace MQTTnet.Server
{
    public interface IMqttClientSession
    {
        /// <summary>
        /// Gets the client identifier.
        /// Hint: This identifier needs to be unique over all used clients / devices on the broker to avoid connection issues.
        /// </summary>
        string ClientId { get; }

        Task StopAsync();
    }
}