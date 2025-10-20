// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Internal;

namespace MQTTnet.Server.Internal;

public class MqttServerEventContainer
{
    public AsyncEvent<ApplicationMessageNotConsumedEventArgs> ApplicationMessageNotConsumedEvent { get; } = new();

    public AsyncEvent<ClientAcknowledgedPublishPacketEventArgs> ClientAcknowledgedPublishPacketEvent { get; } = new();

    public AsyncEvent<ClientConnectedEventArgs> ClientConnectedEvent { get; } = new();

    public AsyncEvent<ClientDisconnectedEventArgs> ClientDisconnectedEvent { get; } = new();

    public AsyncEvent<ClientSubscribedTopicEventArgs> ClientSubscribedTopicEvent { get; } = new();

    public AsyncEvent<ClientUnsubscribedTopicEventArgs> ClientUnsubscribedTopicEvent { get; } = new();

    public AsyncEvent<InterceptingClientApplicationMessageEnqueueEventArgs> InterceptingClientEnqueueEvent { get; } = new();

    public AsyncEvent<ApplicationMessageEnqueuedEventArgs> ApplicationMessageEnqueuedOrDroppedEvent { get; } = new();

    public AsyncEvent<QueueMessageOverwrittenEventArgs> QueuedApplicationMessageOverwrittenEvent { get; } = new();

    public AsyncEvent<InterceptingPacketEventArgs> InterceptingInboundPacketEvent { get; } = new();

    public AsyncEvent<InterceptingPacketEventArgs> InterceptingOutboundPacketEvent { get; } = new();

    public AsyncEvent<InterceptingPublishEventArgs> InterceptingPublishEvent { get; } = new();

    public AsyncEvent<InterceptingSubscriptionEventArgs> InterceptingSubscriptionEvent { get; } = new();

    public AsyncEvent<InterceptingUnsubscriptionEventArgs> InterceptingUnsubscriptionEvent { get; } = new();

    public AsyncEvent<LoadingRetainedMessagesEventArgs> LoadingRetainedMessagesEvent { get; } = new();

    public AsyncEvent<EventArgs> PreparingSessionEvent { get; } = new();

    public AsyncEvent<RetainedMessageChangedEventArgs> RetainedMessageChangedEvent { get; } = new();

    public AsyncEvent<EventArgs> RetainedMessagesClearedEvent { get; } = new();

    public AsyncEvent<SessionDeletedEventArgs> SessionDeletedEvent { get; } = new();

    public AsyncEvent<EventArgs> StartedEvent { get; } = new();

    public AsyncEvent<EventArgs> StoppedEvent { get; } = new();

    public AsyncEvent<ValidatingConnectionEventArgs> ValidatingConnectionEvent { get; } = new();

    public async Task<bool> ShouldSkipEnqueue(string senderClientId, string clientId, MqttApplicationMessage applicationMessage)
    {
        if (!InterceptingClientEnqueueEvent.HasHandlers)
        {
            return false;
        }

        var eventArgs = new InterceptingClientApplicationMessageEnqueueEventArgs(senderClientId, clientId, applicationMessage);
        await InterceptingClientEnqueueEvent.InvokeAsync(eventArgs).ConfigureAwait(false);

        return !eventArgs.AcceptEnqueue;
    }
}