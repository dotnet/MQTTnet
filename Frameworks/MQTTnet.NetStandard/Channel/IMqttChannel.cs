using System;
using System.IO;
using System.Threading.Tasks;

namespace MQTTnet.Channel
{
    public interface IMqttChannel : IDisposable
    {
        Stream SendStream { get; }
        Stream ReceiveStream { get; }

        Task ConnectAsync();
        Task DisconnectAsync();
    }
}
