// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using MQTTnet.Internal;

namespace MQTTnet.Server
{
    public sealed class MqttServerEventContainer
    {
        public AsyncEvent<ApplicationMessageNotConsumedEventArgs> ApplicationMessageNotConsumedEvent { get; } = new AsyncEvent<ApplicationMessageNotConsumedEventArgs>();

        public AsyncEvent<ClientAcknowledgedPublishPacketEventArgs> ClientAcknowledgedPublishPacketEvent { get; } = new AsyncEvent<ClientAcknowledgedPublishPacketEventArgs>();

        public AsyncEvent<ClientConnectedEventArgs> ClientConnectedEvent { get; } = new AsyncEvent<ClientConnectedEventArgs>();

        public AsyncEvent<ClientDisconnectedEventArgs> ClientDisconnectedEvent { get; } = new AsyncEvent<ClientDisconnectedEventArgs>();

        public AsyncEvent<ClientReAuthenticatingEventArgs> ClientReAuthenticatingEvent { get; } = new AsyncEvent<ClientReAuthenticatingEventArgs>();

        public AsyncEvent<ClientSubscribedTopicEventArgs> ClientSubscribedTopicEvent { get; } = new AsyncEvent<ClientSubscribedTopicEventArgs>();

        public AsyncEvent<ClientUnsubscribedTopicEventArgs> ClientUnsubscribedTopicEvent { get; } = new AsyncEvent<ClientUnsubscribedTopicEventArgs>();

        public AsyncEvent<InterceptingClientApplicationMessageEnqueueEventArgs> InterceptingClientEnqueueEvent { get; } =
            new AsyncEvent<InterceptingClientApplicationMessageEnqueueEventArgs>();

        public AsyncEvent<InterceptingPacketEventArgs> InterceptingInboundPacketEvent { get; } = new AsyncEvent<InterceptingPacketEventArgs>();

        public AsyncEvent<InterceptingPacketEventArgs> InterceptingOutboundPacketEvent { get; } = new AsyncEvent<InterceptingPacketEventArgs>();

        public AsyncEvent<InterceptingPublishEventArgs> InterceptingPublishEvent { get; } = new AsyncEvent<InterceptingPublishEventArgs>();

        public AsyncEvent<InterceptingSubscriptionEventArgs> InterceptingSubscriptionEvent { get; } = new AsyncEvent<InterceptingSubscriptionEventArgs>();

        public AsyncEvent<InterceptingUnsubscriptionEventArgs> InterceptingUnsubscriptionEvent { get; } = new AsyncEvent<InterceptingUnsubscriptionEventArgs>();

        public AsyncEvent<LoadingRetainedMessagesEventArgs> LoadingRetainedMessagesEvent { get; } = new AsyncEvent<LoadingRetainedMessagesEventArgs>();

        public AsyncEvent<EventArgs> PreparingSessionEvent { get; } = new AsyncEvent<EventArgs>();

        public AsyncEvent<RetainedMessageChangedEventArgs> RetainedMessageChangedEvent { get; } = new AsyncEvent<RetainedMessageChangedEventArgs>();

        public AsyncEvent<EventArgs> RetainedMessagesClearedEvent { get; } = new AsyncEvent<EventArgs>();

        public AsyncEvent<SessionDeletedEventArgs> SessionDeletedEvent { get; } = new AsyncEvent<SessionDeletedEventArgs>();

        public AsyncEvent<EventArgs> StartedEvent { get; } = new AsyncEvent<EventArgs>();

        public AsyncEvent<EventArgs> StoppedEvent { get; } = new AsyncEvent<EventArgs>();

        public AsyncEvent<ValidatingConnectionEventArgs> ValidatingConnectionEvent { get; } = new AsyncEvent<ValidatingConnectionEventArgs>();
    }
}