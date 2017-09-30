using System;
using System.Threading.Tasks;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Server;

namespace MQTTnet.Core.Tests
{
    public class TestMqttServerAdapter : IMqttServerAdapter
    {
        public event EventHandler<MqttClientConnectedEventArgs> ClientConnected;

        public void FireClientConnectedEvent(MqttClientConnectedEventArgs eventArgs)
        {
            ClientConnected?.Invoke(this, eventArgs);
        }

        public Task StartAsync(MqttServerOptions options)
        {
            return Task.FromResult(0);
        }

        public Task StopAsync()
        {
            return Task.FromResult(0);
        }
    }
}