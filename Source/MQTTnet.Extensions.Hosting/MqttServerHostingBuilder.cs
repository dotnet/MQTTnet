using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Extensions.Hosting.Events;
using MQTTnet.Extensions.Hosting.Options;
using MQTTnet.Server;

namespace MQTTnet.Extensions.Hosting
{
    public class MqttServerHostingBuilder : MqttServerOptionsBuilder
    {
        readonly MqttServerHostingOptions _hostingOptions;
        readonly List<Action<MqttServer>> _startActions;
        readonly List<Action<MqttServer>> _stopActions;

        public MqttServerHostingBuilder(IServiceProvider serviceProvider, List<Action<MqttServer>> startActions, List<Action<MqttServer>> stopActions)
        {
            ServiceProvider = serviceProvider;
            _hostingOptions = serviceProvider.GetRequiredService<MqttServerHostingOptions>();
            _startActions = startActions;
            _stopActions = stopActions;
        }

        public event Func<ApplicationMessageNotConsumedEventArgs, Task> ApplicationMessageNotConsumedAsync
        {
            add
            {
                _startActions.Add(server => server.ApplicationMessageNotConsumedAsync += value);
                if (_hostingOptions.AutoRemoveEventHandlers)
                {
                    _stopActions.Add(server => server.ApplicationMessageNotConsumedAsync -= value);
                }
            }
            remove => _startActions.Add(server => server.ApplicationMessageNotConsumedAsync -= value);
        }

        public event Func<ClientAcknowledgedPublishPacketEventArgs, Task> ClientAcknowledgedPublishPacketAsync
        {
            add
            {
                _startActions.Add(server => server.ClientAcknowledgedPublishPacketAsync += value);
                if (_hostingOptions.AutoRemoveEventHandlers)
                {
                    _stopActions.Add(server => server.ClientAcknowledgedPublishPacketAsync -= value);
                }
            }
            remove => _startActions.Add(server => server.ClientAcknowledgedPublishPacketAsync -= value);
        }

        public event Func<ClientConnectedEventArgs, Task> ClientConnectedAsync
        {
            add
            {
                _startActions.Add(server => server.ClientConnectedAsync += value);
                if (_hostingOptions.AutoRemoveEventHandlers)
                {
                    _stopActions.Add(server => server.ClientConnectedAsync -= value);
                }
            }
            remove => _startActions.Add(server => server.ClientConnectedAsync -= value);
        }

        public event Func<ClientDisconnectedEventArgs, Task> ClientDisconnectedAsync
        {
            add
            {
                _startActions.Add(server => server.ClientDisconnectedAsync += value);
                if (_hostingOptions.AutoRemoveEventHandlers)
                {
                    _stopActions.Add(server => server.ClientDisconnectedAsync -= value);
                }
            }
            remove => _startActions.Add(server => server.ClientDisconnectedAsync -= value);
        }

        public event Func<ClientSubscribedTopicEventArgs, Task> ClientSubscribedTopicAsync
        {
            add
            {
                _startActions.Add(server => server.ClientSubscribedTopicAsync += value);
                if (_hostingOptions.AutoRemoveEventHandlers)
                {
                    _stopActions.Add(server => server.ClientSubscribedTopicAsync -= value);
                }
            }
            remove => _startActions.Add(server => server.ClientSubscribedTopicAsync -= value);
        }

        public event Func<ClientUnsubscribedTopicEventArgs, Task> ClientUnsubscribedTopicAsync
        {
            add
            {
                _startActions.Add(server => server.ClientUnsubscribedTopicAsync += value);
                if (_hostingOptions.AutoRemoveEventHandlers)
                {
                    _stopActions.Add(server => server.ClientUnsubscribedTopicAsync -= value);
                }
            }
            remove => _startActions.Add(server => server.ClientUnsubscribedTopicAsync -= value);
        }

        public event Func<InterceptingPacketEventArgs, Task> InterceptingInboundPacketAsync
        {
            add
            {
                _startActions.Add(server => server.InterceptingInboundPacketAsync += value);
                if (_hostingOptions.AutoRemoveEventHandlers)
                {
                    _stopActions.Add(server => server.InterceptingInboundPacketAsync -= value);
                }
            }
            remove => _startActions.Add(server => server.InterceptingInboundPacketAsync -= value);
        }

        public event Func<InterceptingPacketEventArgs, Task> InterceptingOutboundPacketAsync
        {
            add
            {
                _startActions.Add(server => server.InterceptingOutboundPacketAsync += value);
                if (_hostingOptions.AutoRemoveEventHandlers)
                {
                    _stopActions.Add(server => server.InterceptingOutboundPacketAsync -= value);
                }
            }
            remove => _startActions.Add(server => server.InterceptingOutboundPacketAsync -= value);
        }

        public event Func<InterceptingPublishEventArgs, Task> InterceptingPublishAsync
        {
            add
            {
                _startActions.Add(server => server.InterceptingPublishAsync += value);
                if (_hostingOptions.AutoRemoveEventHandlers)
                {
                    _stopActions.Add(server => server.InterceptingPublishAsync -= value);
                }
            }
            remove => _startActions.Add(server => server.InterceptingPublishAsync -= value);
        }

        public event Func<InterceptingSubscriptionEventArgs, Task> InterceptingSubscriptionAsync
        {
            add
            {
                _startActions.Add(server => server.InterceptingSubscriptionAsync += value);
                if (_hostingOptions.AutoRemoveEventHandlers)
                {
                    _stopActions.Add(server => server.InterceptingSubscriptionAsync -= value);
                }
            }
            remove => _startActions.Add(server => server.InterceptingSubscriptionAsync -= value);
        }

        public event Func<InterceptingUnsubscriptionEventArgs, Task> InterceptingUnsubscriptionAsync
        {
            add
            {
                _startActions.Add(server => server.InterceptingUnsubscriptionAsync += value);
                if (_hostingOptions.AutoRemoveEventHandlers)
                {
                    _stopActions.Add(server => server.InterceptingUnsubscriptionAsync -= value);
                }
            }
            remove => _startActions.Add(server => server.InterceptingUnsubscriptionAsync -= value);
        }

        public event Func<LoadingRetainedMessagesEventArgs, Task> LoadingRetainedMessageAsync
        {
            add
            {
                _startActions.Add(server => server.LoadingRetainedMessageAsync += value);
                if (_hostingOptions.AutoRemoveEventHandlers)
                {
                    _stopActions.Add(server => server.LoadingRetainedMessageAsync -= value);
                }
            }
            remove => _startActions.Add(server => server.LoadingRetainedMessageAsync -= value);
        }

        public event Func<EventArgs, Task> PreparingSessionAsync
        {
            add
            {
                _startActions.Add(server => server.PreparingSessionAsync += value);
                if (_hostingOptions.AutoRemoveEventHandlers)
                {
                    _stopActions.Add(server => server.PreparingSessionAsync -= value);
                }
            }
            remove => _startActions.Add(server => server.PreparingSessionAsync -= value);
        }

        public event Func<RetainedMessageChangedEventArgs, Task> RetainedMessageChangedAsync
        {
            add
            {
                _startActions.Add(server => server.RetainedMessageChangedAsync += value);
                if (_hostingOptions.AutoRemoveEventHandlers)
                {
                    _stopActions.Add(server => server.RetainedMessageChangedAsync -= value);
                }
            }
            remove => _startActions.Add(server => server.RetainedMessageChangedAsync -= value);
        }

        public event Func<EventArgs, Task> RetainedMessagesClearedAsync
        {
            add
            {
                _startActions.Add(server => server.RetainedMessagesClearedAsync += value);
                if (_hostingOptions.AutoRemoveEventHandlers)
                {
                    _stopActions.Add(server => server.RetainedMessagesClearedAsync -= value);
                }
            }
            remove => _startActions.Add(server => server.RetainedMessagesClearedAsync -= value);
        }

        public event Func<SessionDeletedEventArgs, Task> SessionDeletedAsync
        {
            add
            {
                _startActions.Add(server => server.SessionDeletedAsync += value);
                if (_hostingOptions.AutoRemoveEventHandlers)
                {
                    _stopActions.Add(server => server.SessionDeletedAsync -= value);
                }
            }
            remove => _startActions.Add(server => server.SessionDeletedAsync -= value);
        }

        public event Func<EventArgs, Task> StartedAsync
        {
            add
            {
                _startActions.Add(server => server.StartedAsync += value);
                if (_hostingOptions.AutoRemoveEventHandlers)
                {
                    _stopActions.Add(server => server.StartedAsync -= value);
                }
            }
            remove => _startActions.Add(server => server.StartedAsync -= value);
        }

        public event Func<EventArgs, Task> StoppedAsync
        {
            add
            {
                _startActions.Add(server => server.StoppedAsync += value);
                if (_hostingOptions.AutoRemoveEventHandlers)
                {
                    _stopActions.Add(server => server.StoppedAsync -= value);
                }
            }
            remove => _startActions.Add(server => server.StoppedAsync -= value);
        }

        public event Func<ValidatingConnectionEventArgs, Task> ValidatingConnectionAsync
        {
            add
            {
                _startActions.Add(server => server.ValidatingConnectionAsync += value);
                if (_hostingOptions.AutoRemoveEventHandlers)
                {
                    _stopActions.Add(server => server.ValidatingConnectionAsync -= value);
                }
            }
            remove => _startActions.Add(server => server.ValidatingConnectionAsync -= value);
        }

        public IServiceProvider ServiceProvider { get; }

        public MqttServerHostingBuilder WithDefaultWebSocketEndpoint()
        {
            _hostingOptions.DefaultWebSocketEndpointOptions.IsEnabled = true;

            return this;
        }

        public MqttServerHostingBuilder WithDefaultWebSocketEndpointBoundIPAddress(IPAddress value)
        {
            _hostingOptions.DefaultWebSocketEndpointOptions.BoundInterNetworkAddress = value;

            return this;
        }

        public MqttServerHostingBuilder WithDefaultWebSocketEndpointBoundIPV6Address(IPAddress value)
        {
            _hostingOptions.DefaultWebSocketEndpointOptions.BoundInterNetworkV6Address = value;

            return this;
        }

        public MqttServerHostingBuilder WithDefaultWebSocketEndpointPort(int value)
        {
            _hostingOptions.DefaultWebSocketEndpointOptions.Port = value;

            return this;
        }

        public MqttServerHostingBuilder WithEncryptedWebSocketEndpoint()
        {
            _hostingOptions.DefaultTlsWebSocketEndpointOptions.IsEnabled = true;

            return this;
        }

        public MqttServerHostingBuilder WithEncryptedWebSocketEndpointBoundIPAddress(IPAddress value)
        {
            _hostingOptions.DefaultTlsWebSocketEndpointOptions.BoundInterNetworkAddress = value;

            return this;
        }

        public MqttServerHostingBuilder WithEncryptedWebSocketEndpointBoundIPV6Address(IPAddress value)
        {
            _hostingOptions.DefaultTlsWebSocketEndpointOptions.BoundInterNetworkV6Address = value;

            return this;
        }

        public MqttServerHostingBuilder WithEncryptedWebSocketEndpointPort(int value)
        {
            _hostingOptions.DefaultTlsWebSocketEndpointOptions.Port = value;

            return this;
        }

        public MqttServerHostingBuilder WithoutDefaultWebSocketEndpoint()
        {
            _hostingOptions.DefaultWebSocketEndpointOptions.IsEnabled = false;

            return this;
        }
    }
}