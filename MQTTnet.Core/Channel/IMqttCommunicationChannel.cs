using System.Threading.Tasks;
using MQTTnet.Core.Client;
using System;

namespace MQTTnet.Core.Channel
{
    public interface IMqttCommunicationChannel
    {
        Task ConnectAsync(MqttClientOptions options);

        Task DisconnectAsync();

        Task WriteAsync(byte[] buffer);

        /// <summary>
        /// get the currently available number of bytes without reading them
        /// </summary>
        int Peek();

        Task<ArraySegment<byte>> ReadAsync(int length, byte[] buffer);
    }
}
