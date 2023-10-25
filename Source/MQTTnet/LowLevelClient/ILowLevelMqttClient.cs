using System;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Packets;

namespace MQTTnet.LowLevelClient
{
    public interface ILowLevelMqttClient : IDisposable
    {
        event Func<InspectMqttPacketEventArgs, Task> InspectPacketAsync;
        
        bool IsConnected { get; }
        
        Task ConnectAsync(MqttClientOptions options, CancellationToken cancellationToken = default);
        
        Task DisconnectAsync(CancellationToken cancellationToken = default);
        
        Task<MqttPacket> ReceiveAsync(CancellationToken cancellationToken = default);
        
        Task SendAsync(MqttPacket packet, CancellationToken cancellationToken = default);
    }
}