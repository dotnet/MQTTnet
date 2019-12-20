using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.ExtendedAuthenticationExchange;
using MQTTnet.Client.Options;
using MQTTnet.Client.Publishing;
using MQTTnet.Client.Receiving;
using MQTTnet.Client.Subscribing;
using MQTTnet.Client.Unsubscribing;

namespace MQTTnet.Tests.Mockups
{
    public class TestClientWrapper : IMqttClient
    {
        public TestClientWrapper(IMqttClient implementation, TestContext testContext)
        {
            Implementation = implementation;
            TestContext = testContext;
        }

        public IMqttClient Implementation { get; }
        public TestContext TestContext { get; }

        public bool IsConnected => Implementation.IsConnected;

        public IMqttClientOptions Options => Implementation.Options;

        public IMqttClientConnectedHandler ConnectedHandler { get => Implementation.ConnectedHandler; set => Implementation.ConnectedHandler = value; }
        public IMqttClientDisconnectedHandler DisconnectedHandler { get => Implementation.DisconnectedHandler; set => Implementation.DisconnectedHandler = value; }
        public IMqttApplicationMessageReceivedHandler ApplicationMessageReceivedHandler { get => Implementation.ApplicationMessageReceivedHandler; set => Implementation.ApplicationMessageReceivedHandler = value; }

        public Task<MqttClientAuthenticateResult> ConnectAsync(IMqttClientOptions options, CancellationToken cancellationToken)
        {
            switch (options)
            {
                case MqttClientOptionsBuilder builder:
                    {
                        var existingClientId = builder.Build().ClientId;
                        if (!existingClientId.StartsWith(TestContext.TestName))
                        {
                            builder.WithClientId(TestContext.TestName + existingClientId);
                        }
                    }
                    break;
                case MqttClientOptions op:
                    {
                        var existingClientId = op.ClientId;
                        if (!existingClientId.StartsWith(TestContext.TestName))
                        {
                            op.ClientId = TestContext.TestName + existingClientId;
                        }
                    }
                    break;
                default:
                    break;
            }

            return Implementation.ConnectAsync(options, cancellationToken);
        }

        public Task DisconnectAsync(MqttClientDisconnectOptions options, CancellationToken cancellationToken)
        {
            return Implementation.DisconnectAsync(options, cancellationToken);
        }

        public void Dispose()
        {
            Implementation.Dispose();
        }

        public Task<MqttClientPublishResult> PublishAsync(MqttApplicationMessage applicationMessage, CancellationToken cancellationToken)
        {
            return Implementation.PublishAsync(applicationMessage, cancellationToken);
        }

        public Task SendExtendedAuthenticationExchangeDataAsync(MqttExtendedAuthenticationExchangeData data, CancellationToken cancellationToken)
        {
            return Implementation.SendExtendedAuthenticationExchangeDataAsync(data, cancellationToken);
        }

        public Task<Client.Subscribing.MqttClientSubscribeResult> SubscribeAsync(MqttClientSubscribeOptions options, CancellationToken cancellationToken)
        {
            return Implementation.SubscribeAsync(options, cancellationToken);
        }

        public Task<MqttClientUnsubscribeResult> UnsubscribeAsync(MqttClientUnsubscribeOptions options, CancellationToken cancellationToken)
        {
            return Implementation.UnsubscribeAsync(options, cancellationToken);
        }
    }
}