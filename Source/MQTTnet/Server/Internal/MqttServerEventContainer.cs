using System;
using MQTTnet.Internal;

namespace MQTTnet.Server
{
    public sealed class MqttServerEventContainer
    {
        public AsyncEvent<InterceptingSubscriptionEventArgs> InterceptingSubscriptionEvent { get; } = new AsyncEvent<InterceptingSubscriptionEventArgs>();
        
        public AsyncEvent<InterceptingUnsubscriptionEventArgs> InterceptingUnsubscriptionEvent { get; } = new AsyncEvent<InterceptingUnsubscriptionEventArgs>();
        
        public AsyncEvent<InterceptingPublishEventArgs> InterceptingPublishEvent { get; } = new AsyncEvent<InterceptingPublishEventArgs>();
        
        public AsyncEvent<ValidatingConnectionEventArgs> ValidatingConnectionEvent { get; } = new AsyncEvent<ValidatingConnectionEventArgs>();
        
        public AsyncEvent<ClientConnectedEventArgs> ClientConnectedEvent { get; } = new AsyncEvent<ClientConnectedEventArgs>();
        
        public AsyncEvent<ClientDisconnectedEventArgs> ClientDisconnectedEvent { get; } = new AsyncEvent<ClientDisconnectedEventArgs>();
        
        public AsyncEvent<ClientSubscribedTopicEventArgs> ClientSubscribedTopicEvent { get; } = new AsyncEvent<ClientSubscribedTopicEventArgs>();
        
        public AsyncEvent<ClientUnsubscribedTopicEventArgs> ClientUnsubscribedTopicEvent { get; } = new AsyncEvent<ClientUnsubscribedTopicEventArgs>();
        
        public AsyncEvent<PreparingSessionEventArgs> PreparingSessionEvent { get; } = new AsyncEvent<PreparingSessionEventArgs>();
        
        public AsyncEvent<ApplicationMessageNotConsumedEventArgs> ApplicationMessageNotConsumedEvent { get; } = new AsyncEvent<ApplicationMessageNotConsumedEventArgs>();
        
        public AsyncEvent<RetainedMessageChangedEventArgs> RetainedMessageChangedEvent { get; } = new AsyncEvent<RetainedMessageChangedEventArgs>();
        
        public AsyncEvent<LoadingRetainedMessagesEventArgs> LoadingRetainedMessagesEvent { get; } = new AsyncEvent<LoadingRetainedMessagesEventArgs>();
        
        public AsyncEvent<EventArgs> RetainedMessagesClearedEvent { get; } = new AsyncEvent<EventArgs>();
        
        public AsyncEvent<InterceptingPacketEventArgs> InterceptingInboundPacketEvent { get; } = new AsyncEvent<InterceptingPacketEventArgs>();
        
        public AsyncEvent<InterceptingPacketEventArgs> InterceptingOutboundPacketEvent { get; } = new AsyncEvent<InterceptingPacketEventArgs>();
        
        public AsyncEvent<EventArgs> StartedEvent { get; } = new AsyncEvent<EventArgs>();
        
        public AsyncEvent<EventArgs> StoppedEvent { get; } = new AsyncEvent<EventArgs>();
    }
}
