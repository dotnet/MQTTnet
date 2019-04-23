using System.Threading.Tasks;

namespace MQTTnet.Server.Status
{
    public interface IMqttSessionStatus
    {
        string ClientId { get; set; }

        long PendingApplicationMessagesCount { get; set; }

        Task ClearPendingApplicationMessagesAsync();

        Task DeleteAsync();
    }
}