using System;
using System.Threading.Tasks;
using MQTTnet.Adapter;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Server;

namespace MQTTnet.Core.Tests
{
    public class TestMqttServerAdapter : IMqttServerAdapter
    {
        public event EventHandler<MqttServerAdapterClientAcceptedEventArgs> ClientAccepted;
        
        public async Task<IMqttClient> ConnectTestClient(IMqttServer server, string clientId, MqttApplicationMessage willMessage = null)
        {
            var adapterA = new TestMqttCommunicationAdapter();
            var adapterB = new TestMqttCommunicationAdapter();
            adapterA.Partner = adapterB;
            adapterB.Partner = adapterA;

            var client = new MqttClient(
                new TestMqttCommunicationAdapterFactory(adapterA),
                new MqttNetLogger());

            var connected = WaitForClientToConnect(server, clientId);

            FireClientAcceptedEvent(adapterB);

            var options = new MqttClientOptions
            {
                ClientId = clientId,
                WillMessage = willMessage,
                ChannelOptions = new MqttClientTcpOptions()
            };

            await client.ConnectAsync(options);
            await connected;

            return client;
        }
        
        private static Task WaitForClientToConnect(IMqttServer s, string clientId)
        {
            var tcs = new TaskCompletionSource<object>();

            void Handler(object sender, Server.MqttClientConnectedEventArgs args)
            {
                if (args.Client.ClientId == clientId)
                {
                    s.ClientConnected -= Handler;
                    tcs.SetResult(null);
                }
            }

            s.ClientConnected += Handler;

            return tcs.Task;
        }

        private void FireClientAcceptedEvent(IMqttChannelAdapter adapter)
        {
            ClientAccepted?.Invoke(this, new MqttServerAdapterClientAcceptedEventArgs(adapter));
        }

        public Task StartAsync(IMqttServerOptions options)
        {
            return Task.FromResult(0);
        }

        public Task StopAsync()
        {
            return Task.FromResult(0);
        }

        public void Dispose()
        {
        }
    }
}