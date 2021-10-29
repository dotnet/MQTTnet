using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Tests.Mockups
{
    public sealed class TestClientWrapper : IMqttClient
    {
        public TestClientWrapper(IMqttClient implementation, TestContext testContext)
        {
            Implementation = implementation ?? throw new ArgumentNullException(nameof(implementation));
            TestContext = testContext;
        }

        public IMqttClient Implementation { get; }

        public TestContext TestContext { get; }

        public bool IsConnected => Implementation.IsConnected;

        public IMqttClientOptions Options => Implementation.Options;

        public IMqttClientConnectedHandler ConnectedHandler
        {
            get => Implementation.ConnectedHandler;
            set => Implementation.ConnectedHandler = value;
        }

        public event Func<MqttClientConnectedEventArgs, Task> ConnectedAsync
        {
            add => Implementation.ConnectedAsync += value;
            remove => Implementation.ConnectedAsync -= value;
        }

        public IMqttClientDisconnectedHandler DisconnectedHandler
        {
            get => Implementation.DisconnectedHandler;
            set => Implementation.DisconnectedHandler = value;
        }

        public event Func<MqttClientDisconnectedEventArgs, Task> DisconnectedAsync
        {
            add => Implementation.DisconnectedAsync += value;
            remove => Implementation.DisconnectedAsync -= value;
        }
        
        public event Func<MqttApplicationMessageReceivedEventArgs, Task> ApplicationMessageReceivedAsync
        {
            add => Implementation.ApplicationMessageReceivedAsync += value;
            remove => Implementation.ApplicationMessageReceivedAsync -= value;
        }

        public Task<MqttClientConnectResult> ConnectAsync(IMqttClientOptions options, CancellationToken cancellationToken)
        {
            if (TestContext != null)
            {
                var clientOptions = (MqttClientOptions)options;

                var existingClientId = clientOptions.ClientId;
                if (existingClientId != null && !existingClientId.StartsWith(TestContext.TestName))
                {
                    clientOptions.ClientId = TestContext.TestName + "_" + existingClientId;
                }
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

        public Task PingAsync(CancellationToken cancellationToken)
        {
            return Implementation.PingAsync(cancellationToken);
        }

        public Task<MqttClientPublishResult> PublishAsync(MqttApplicationMessage applicationMessage, CancellationToken cancellationToken)
        {
            return Implementation.PublishAsync(applicationMessage, cancellationToken);
        }

        public Task SendExtendedAuthenticationExchangeDataAsync(MqttExtendedAuthenticationExchangeData data, CancellationToken cancellationToken)
        {
            return Implementation.SendExtendedAuthenticationExchangeDataAsync(data, cancellationToken);
        }

        public Task<MqttClientSubscribeResult> SubscribeAsync(MqttClientSubscribeOptions options, CancellationToken cancellationToken)
        {
            return Implementation.SubscribeAsync(options, cancellationToken);
        }

        public Task<MqttClientUnsubscribeResult> UnsubscribeAsync(MqttClientUnsubscribeOptions options, CancellationToken cancellationToken)
        {
            return Implementation.UnsubscribeAsync(options, cancellationToken);
        }
    }
}