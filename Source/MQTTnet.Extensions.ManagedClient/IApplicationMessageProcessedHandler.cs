using System.Threading.Tasks;

namespace MQTTnet.Extensions.ManagedClient
{
    public interface IApplicationMessageProcessedHandler
    {
        Task HandleApplicationMessageProcessedAsync(ApplicationMessageProcessedEventArgs eventArgs);
    }
}
