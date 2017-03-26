using System;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Server;

namespace MQTTnet.Core.Tests
{
    public class TestMqttServerAdapter : IMqttServerAdapter
    {
        public event EventHandler<MqttClientConnectedEventArgs> ClientConnected;

        public void Start(MqttServerOptions options)
        {
        }

        public void Stop()
        {                 
        }
    }
}