using System;
using System.Threading.Tasks;

namespace MQTTnet.Server
{
    public interface IMqttServerStartedHandler
    {
        Task HandleServerStartedAsync(EventArgs eventArgs);
    }
}
