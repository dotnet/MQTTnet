using System;

namespace MQTTnet.Server
{
    public interface IMqttClientSession : IDisposable
    {
        string ClientId { get; }

        void Stop(MqttClientDisconnectType disconnectType);
    }
}