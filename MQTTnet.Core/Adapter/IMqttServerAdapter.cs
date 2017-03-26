using System;
using MQTTnet.Core.Server;

namespace MQTTnet.Core.Adapter
{
    public interface IMqttServerAdapter
    {
        event EventHandler<MqttClientConnectedEventArgs> ClientConnected;

        void Start(MqttServerOptions options);

        void Stop();
    }
}
