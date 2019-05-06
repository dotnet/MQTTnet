using System;
using System.Threading.Tasks;

namespace MQTTnet.Server
{
    public interface IMqttServerStoppedHandler
    {
        Task HandleServerStoppedAsync(EventArgs eventArgs);
    }
}
