using System.Threading.Tasks;

namespace MQTTnet.Extensions.ManagedClient
{
    public interface ISynchronizingSubscriptionsFailedHandler
    {
        Task HandleSynchronizingSubscriptionsFailedAsync(ManagedProcessFailedEventArgs eventArgs);
    }
}
