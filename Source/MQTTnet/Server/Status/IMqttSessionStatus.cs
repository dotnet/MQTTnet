using System.Threading.Tasks;

namespace MQTTnet.Server.Status
{
    public interface IMqttSessionStatus
    {
        string ClientId { get; set; }

        bool IsConnected { get; }

        long PendingApplicationMessagesCount { get; set; }

        Task ClearPendingApplicationMessagesAsync();

        Task DeleteAsync();
    }
}