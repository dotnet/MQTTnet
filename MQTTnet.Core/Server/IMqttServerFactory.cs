using System;

namespace MQTTnet.Core.Server
{
    public interface IMqttServerFactory
    {
        IMqttServer CreateMqttServer();

        IMqttServer CreateMqttServer(Action<MqttServerOptions> configure);
    }
}