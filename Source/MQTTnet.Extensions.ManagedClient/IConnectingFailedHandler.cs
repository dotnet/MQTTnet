using System.Threading.Tasks;

namespace MQTTnet.Extensions.ManagedClient
{
    public interface IConnectingFailedHandler
    {
        Task HandleConnectingFailedAsync(ManagedProcessFailedEventArgs eventArgs);
    }
}
