using System;
using System.Threading.Tasks;
using MQTTnet.Formatter;

namespace MQTTnet.Server
{
    public interface IMqttClientSessionStatus
    {
        string ClientId { get; }

        string Endpoint { get; }

        bool IsConnected { get; }

        MqttProtocolVersion? ProtocolVersion { get; }

        TimeSpan LastPacketReceived { get; }

        TimeSpan LastNonKeepAlivePacketReceived { get; }

        int PendingApplicationMessagesCount { get; }

        Task DisconnectAsync();

        Task DeleteSessionAsync();

        Task ClearPendingApplicationMessagesAsync();
    }
}
