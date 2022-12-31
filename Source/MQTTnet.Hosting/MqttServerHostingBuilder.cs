using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTTnet.Hosting
{
    public class MqttServerHostingBuilder : MqttServerOptionsBuilder
    {
        private readonly List<Action<MqttServer>> _configureActions;

        public MqttServerHostingBuilder(IServiceProvider serviceProvider, List<Action<MqttServer>> configureActions)
        {
            ServiceProvider = serviceProvider;
            _configureActions = configureActions;
        }

        public event Func<ApplicationMessageNotConsumedEventArgs, Task> ApplicationMessageNotConsumedAsync
        {
            add => _configureActions.Add(server => server.ApplicationMessageNotConsumedAsync += value);
            remove => _configureActions.Add(server => server.ApplicationMessageNotConsumedAsync -= value);
        }

        public event Func<ClientAcknowledgedPublishPacketEventArgs, Task> ClientAcknowledgedPublishPacketAsync
        {
            add => _configureActions.Add(server => server.ClientAcknowledgedPublishPacketAsync += value);
            remove => _configureActions.Add(server => server.ClientAcknowledgedPublishPacketAsync -= value);
        }

        public event Func<ClientConnectedEventArgs, Task> ClientConnectedAsync
        {
            add => _configureActions.Add(server => server.ClientConnectedAsync += value);
            remove => _configureActions.Add(server => server.ClientConnectedAsync -= value);
        }

        public event Func<ClientDisconnectedEventArgs, Task> ClientDisconnectedAsync
        {
            add => _configureActions.Add(server => server.ClientDisconnectedAsync += value);
            remove => _configureActions.Add(server => server.ClientDisconnectedAsync -= value);
        }

        public event Func<ClientSubscribedTopicEventArgs, Task> ClientSubscribedTopicAsync
        {
            add => _configureActions.Add(server => server.ClientSubscribedTopicAsync += value);
            remove => _configureActions.Add(server => server.ClientSubscribedTopicAsync -= value);
        }

        public event Func<ClientUnsubscribedTopicEventArgs, Task> ClientUnsubscribedTopicAsync
        {
            add => _configureActions.Add(server => server.ClientUnsubscribedTopicAsync += value);
            remove => _configureActions.Add(server => server.ClientUnsubscribedTopicAsync -= value);
        }

        public event Func<InterceptingPacketEventArgs, Task> InterceptingInboundPacketAsync
        {
            add => _configureActions.Add(server => server.InterceptingInboundPacketAsync += value);
            remove => _configureActions.Add(server => server.InterceptingInboundPacketAsync -= value);
        }

        public event Func<InterceptingPacketEventArgs, Task> InterceptingOutboundPacketAsync
        {
            add => _configureActions.Add(server => server.InterceptingOutboundPacketAsync += value);
            remove => _configureActions.Add(server => server.InterceptingOutboundPacketAsync -= value);
        }

        public event Func<InterceptingPublishEventArgs, Task> InterceptingPublishAsync
        {
            add => _configureActions.Add(server => server.InterceptingPublishAsync += value);
            remove => _configureActions.Add(server => server.InterceptingPublishAsync -= value);
        }

        public event Func<InterceptingSubscriptionEventArgs, Task> InterceptingSubscriptionAsync
        {
            add => _configureActions.Add(server => server.InterceptingSubscriptionAsync += value);
            remove => _configureActions.Add(server => server.InterceptingSubscriptionAsync -= value);
        }

        public event Func<InterceptingUnsubscriptionEventArgs, Task> InterceptingUnsubscriptionAsync
        {
            add => _configureActions.Add(server => server.InterceptingUnsubscriptionAsync += value);
            remove => _configureActions.Add(server => server.InterceptingUnsubscriptionAsync -= value);
        }

        public event Func<LoadingRetainedMessagesEventArgs, Task> LoadingRetainedMessageAsync
        {
            add => _configureActions.Add(server => server.LoadingRetainedMessageAsync += value);
            remove => _configureActions.Add(server => server.LoadingRetainedMessageAsync -= value);
        }

        public event Func<EventArgs, Task> PreparingSessionAsync
        {
            add => _configureActions.Add(server => server.PreparingSessionAsync += value);
            remove => _configureActions.Add(server => server.PreparingSessionAsync -= value);
        }

        public event Func<RetainedMessageChangedEventArgs, Task> RetainedMessageChangedAsync
        {
            add => _configureActions.Add(server => server.RetainedMessageChangedAsync += value);
            remove => _configureActions.Add(server => server.RetainedMessageChangedAsync -= value);
        }

        public event Func<EventArgs, Task> RetainedMessagesClearedAsync
        {
            add => _configureActions.Add(server => server.RetainedMessagesClearedAsync += value);
            remove => _configureActions.Add(server => server.RetainedMessagesClearedAsync -= value);
        }

        public event Func<SessionDeletedEventArgs, Task> SessionDeletedAsync
        {
            add => _configureActions.Add(server => server.SessionDeletedAsync += value);
            remove => _configureActions.Add(server => server.SessionDeletedAsync -= value);
        }

        public event Func<EventArgs, Task> StartedAsync
        {
            add => _configureActions.Add(server => server.StartedAsync += value);
            remove => _configureActions.Add(server => server.StartedAsync -= value);
        }

        public event Func<EventArgs, Task> StoppedAsync
        {
            add => _configureActions.Add(server => server.StoppedAsync += value);
            remove => _configureActions.Add(server => server.StoppedAsync -= value);
        }

        public event Func<ValidatingConnectionEventArgs, Task> ValidatingConnectionAsync
        {
            add => _configureActions.Add(server => server.ValidatingConnectionAsync += value);
            remove => _configureActions.Add(server => server.ValidatingConnectionAsync -= value);
        }

        public IServiceProvider ServiceProvider { get; }

    }
}
