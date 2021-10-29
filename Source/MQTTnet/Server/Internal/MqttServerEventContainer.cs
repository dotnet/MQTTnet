using System;
using MQTTnet.Internal;

namespace MQTTnet.Server
{
    public sealed class MqttServerEventContainer
    {
        public AsyncEvent<InterceptingMqttClientSubscriptionEventArgs> InterceptingClientSubscriptionEvent { get; } = new AsyncEvent<InterceptingMqttClientSubscriptionEventArgs>();
        
        public AsyncEvent<InterceptingMqttClientUnsubscriptionEventArgs> InterceptingClientUnsubscriptionEvent { get; } = new AsyncEvent<InterceptingMqttClientUnsubscriptionEventArgs>();
        
        public AsyncEvent<InterceptingMqttClientPublishEventArgs> InterceptingClientPublishEvent { get; } = new AsyncEvent<InterceptingMqttClientPublishEventArgs>();
        
        public AsyncEvent<ValidatingMqttClientConnectionEventArgs> ValidatingClientConnectionEvent { get; } = new AsyncEvent<ValidatingMqttClientConnectionEventArgs>();
        
        public AsyncEvent<MqttServerClientConnectedEventArgs> ClientConnectedEvent { get; } = new AsyncEvent<MqttServerClientConnectedEventArgs>();
        
        public AsyncEvent<MqttServerClientDisconnectedEventArgs> ClientDisconnectedEvent { get; } = new AsyncEvent<MqttServerClientDisconnectedEventArgs>();
        
        public AsyncEvent<MqttServerClientSubscribedTopicEventArgs> ClientSubscribedTopicEvent { get; } = new AsyncEvent<MqttServerClientSubscribedTopicEventArgs>();
        
        public AsyncEvent<MqttServerClientUnsubscribedTopicEventArgs> ClientUnsubscribedTopicEvent { get; } = new AsyncEvent<MqttServerClientUnsubscribedTopicEventArgs>();
        
        public AsyncEvent<PreparingMqttClientSessionEventArgs> PreparingClientSessionEvent { get; } = new AsyncEvent<PreparingMqttClientSessionEventArgs>();
        
        public AsyncEvent<MqttApplicationMessageNotConsumedEventArgs> ApplicationMessageNotConsumedEvent { get; } = new AsyncEvent<MqttApplicationMessageNotConsumedEventArgs>();
        
        public AsyncEvent<EventArgs> RetainedApplicationMessageChangedEvent { get; } = new AsyncEvent<EventArgs>();
        
        public AsyncEvent<EventArgs> RetainedApplicationMessageClearedEvent { get; } = new AsyncEvent<EventArgs>();
        
        public AsyncEvent<InterceptingPacketEventArgs> InterceptingInboundPacketEvent { get; } = new AsyncEvent<InterceptingPacketEventArgs>();
        
        public AsyncEvent<InterceptingPacketEventArgs> InterceptingOutboundPacketEvent { get; } = new AsyncEvent<InterceptingPacketEventArgs>();
        
        public AsyncEvent<EventArgs> StartedEvent { get; } = new AsyncEvent<EventArgs>();
        
        public AsyncEvent<EventArgs> StoppedEvent { get; } = new AsyncEvent<EventArgs>();
    }
}
