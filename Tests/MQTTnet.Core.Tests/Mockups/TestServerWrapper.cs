using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client.Publishing;
using MQTTnet.Client.Receiving;
using MQTTnet.Server;
using MQTTnet.Server.Status;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Tests.Mockups
{
    public sealed class TestServerWrapper : IMqttServer
    {
        public TestServerWrapper(IMqttServer implementation, TestContext testContext, TestEnvironment testEnvironment)
        {
            Implementation = implementation;
            TestContext = testContext;
            TestEnvironment = testEnvironment;
        }

        public IMqttServer Implementation { get; }
        public TestContext TestContext { get; }
        public TestEnvironment TestEnvironment { get; }

        public IMqttServerStartedHandler StartedHandler
        {
            get => Implementation.StartedHandler;
            set => Implementation.StartedHandler = value;
        }

        public IMqttServerStoppedHandler StoppedHandler
        {
            get => Implementation.StoppedHandler;
            set => Implementation.StoppedHandler = value;
        }

        public IMqttServerClientConnectedHandler ClientConnectedHandler
        {
            get => Implementation.ClientConnectedHandler;
            set => Implementation.ClientConnectedHandler = value;
        }

        public IMqttServerClientDisconnectedHandler ClientDisconnectedHandler
        {
            get => Implementation.ClientDisconnectedHandler;
            set => Implementation.ClientDisconnectedHandler = value;
        }

        public IMqttServerClientSubscribedTopicHandler ClientSubscribedTopicHandler
        {
            get => Implementation.ClientSubscribedTopicHandler;
            set => Implementation.ClientSubscribedTopicHandler = value;
        }

        public IMqttServerClientUnsubscribedTopicHandler ClientUnsubscribedTopicHandler
        {
            get => Implementation.ClientUnsubscribedTopicHandler;
            set => Implementation.ClientUnsubscribedTopicHandler = value;
        }

        public IMqttServerOptions Options => Implementation.Options;

        public IMqttApplicationMessageReceivedHandler ApplicationMessageReceivedHandler
        {
            get => Implementation.ApplicationMessageReceivedHandler;
            set => Implementation.ApplicationMessageReceivedHandler = value;
        }

        public Task ClearRetainedApplicationMessagesAsync()
        {
            return Implementation.ClearRetainedApplicationMessagesAsync();
        }

        public Task<IList<IMqttClientStatus>> GetClientStatusAsync()
        {
            return Implementation.GetClientStatusAsync();
        }

        public Task<IList<MqttApplicationMessage>> GetRetainedApplicationMessagesAsync()
        {
            return Implementation.GetRetainedApplicationMessagesAsync();
        }

        public Task<IList<IMqttSessionStatus>> GetSessionStatusAsync()
        {
            return Implementation.GetSessionStatusAsync();
        }

        public Task<MqttClientPublishResult> PublishAsync(MqttApplicationMessage applicationMessage, CancellationToken cancellationToken)
        {
            return Implementation.PublishAsync(applicationMessage, cancellationToken);
        }

        public Task StartAsync(IMqttServerOptions options)
        {
            if (TestContext != null)
            {
                var serverOptions = (MqttServerOptions)options;

                if (serverOptions.ConnectionValidator == null)
                {
                    serverOptions.ConnectionValidator = new MqttServerConnectionValidatorDelegate(ConnectionValidator);
                }
            }

            return Implementation.StartAsync(options);
        }

        public void ConnectionValidator(MqttConnectionValidatorContext ctx)
        {
            if (!ctx.ClientId.StartsWith(TestContext.TestName))
            {
                TestEnvironment.TrackException(new InvalidOperationException($"Invalid client ID used ({ctx.ClientId}). It must start with UnitTest name."));
                ctx.ReasonCode = Protocol.MqttConnectReasonCode.ClientIdentifierNotValid;
            }
        }

        public Task StopAsync()
        {
            return Implementation.StopAsync();
        }

        public Task SubscribeAsync(string clientId, ICollection<TopicFilter> topicFilters)
        {
            return Implementation.SubscribeAsync(clientId, topicFilters);
        }

        public Task UnsubscribeAsync(string clientId, ICollection<string> topicFilters)
        {
            return Implementation.UnsubscribeAsync(clientId, topicFilters);
        }
    }
}