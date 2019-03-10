using System.Threading.Tasks;

namespace MQTTnet.Extensions.ManagedClient
{
    public interface IApplicationMessageSkippedHandler
    {
        Task HandleApplicationMessageSkippedAsync(ApplicationMessageSkippedEventArgs eventArgs);
    }
}
