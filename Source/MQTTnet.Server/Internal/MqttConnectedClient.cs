// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Adapter;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Server.Internal.Formatter;
using MqttDisconnectPacketFactory = MQTTnet.Server.Internal.Formatter.MqttDisconnectPacketFactory;
using MqttPubAckPacketFactory = MQTTnet.Server.Internal.Formatter.MqttPubAckPacketFactory;
using MqttPubCompPacketFactory = MQTTnet.Server.Internal.Formatter.MqttPubCompPacketFactory;
using MqttPublishPacketFactory = MQTTnet.Server.Internal.Formatter.MqttPublishPacketFactory;
using MqttPubRecPacketFactory = MQTTnet.Server.Internal.Formatter.MqttPubRecPacketFactory;
using MqttPubRelPacketFactory = MQTTnet.Server.Internal.Formatter.MqttPubRelPacketFactory;

namespace MQTTnet.Server.Internal;

public sealed class MqttConnectedClient : IDisposable
{
    readonly MqttServerEventContainer _eventContainer;
    readonly MqttNetSourceLogger _logger;
    readonly MqttServerOptions _serverOptions;
    readonly MqttClientSessionsManager _sessionsManager;
    readonly Dictionary<ushort, string> _topicAlias = new();

    CancellationTokenSource _cancellationToken = new();
    bool _disconnectPacketSent;

    public MqttConnectedClient(
        MqttConnectPacket connectPacket,
        IMqttChannelAdapter channelAdapter,
        MqttSession session,
        MqttServerOptions serverOptions,
        MqttServerEventContainer eventContainer,
        MqttClientSessionsManager sessionsManager,
        IMqttNetLogger logger)
    {
        _serverOptions = serverOptions ?? throw new ArgumentNullException(nameof(serverOptions));
        _eventContainer = eventContainer ?? throw new ArgumentNullException(nameof(eventContainer));
        _sessionsManager = sessionsManager ?? throw new ArgumentNullException(nameof(sessionsManager));
        ConnectPacket = connectPacket ?? throw new ArgumentNullException(nameof(connectPacket));

        ChannelAdapter = channelAdapter ?? throw new ArgumentNullException(nameof(channelAdapter));
        Endpoint = channelAdapter.Endpoint;
        Session = session ?? throw new ArgumentNullException(nameof(session));

        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger.WithSource(nameof(MqttConnectedClient));
    }

    public IMqttChannelAdapter ChannelAdapter { get; }

    public MqttConnectPacket ConnectPacket { get; }

    public MqttDisconnectPacket DisconnectPacket { get; private set; }

    public string Endpoint { get; }

    public string Id => ConnectPacket.ClientId;

    public bool IsRunning { get; private set; }

    public bool IsTakenOver { get; set; }

    public MqttSession Session { get; }

    public MqttClientStatistics Statistics { get; } = new();

    public void Dispose()
    {
        _cancellationToken?.Dispose();
    }

    public void ResetStatistics()
    {
        ChannelAdapter.ResetStatistics();
        Statistics.ResetStatistics();
    }

    public async Task RunAsync()
    {
        _logger.Info("Client '{0}': Session started", Id);

        Session.LatestConnectPacket = ConnectPacket;
        Session.WillMessageSent = false;

        try
        {
            var cancellationToken = _cancellationToken.Token;
            IsRunning = true;

            _ = Task.Factory.StartNew(() => SendPacketsLoop(cancellationToken), cancellationToken, TaskCreationOptions.PreferFairness, TaskScheduler.Default).ConfigureAwait(false);

            await ReceivePackagesLoop(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            IsRunning = false;

            Session.DisconnectedTimestamp = DateTime.UtcNow;

            _cancellationToken?.TryCancel();
            _cancellationToken?.Dispose();
            _cancellationToken = null;
        }

        var isCleanDisconnect = DisconnectPacket != null;

        if (!IsTakenOver && !isCleanDisconnect && Session.LatestConnectPacket.WillFlag && !Session.WillMessageSent)
        {
            var willPublishPacket = MqttPublishPacketFactory.Create(Session.LatestConnectPacket);
            var willApplicationMessage = MqttApplicationMessageFactory.Create(willPublishPacket);

            _ = _sessionsManager.DispatchApplicationMessage(Id, Session.Items, willApplicationMessage, CancellationToken.None);
            Session.WillMessageSent = true;

            _logger.Info("Client '{0}': Published will message", Id);
        }

        _logger.Info("Client '{0}': Connection stopped", Id);
    }

    public async Task SendPacketAsync(MqttPacket packet, CancellationToken cancellationToken)
    {
        packet = await InterceptPacketAsync(packet, cancellationToken).ConfigureAwait(false);
        if (packet == null)
        {
            // The interceptor has decided that this packet will not used at all.
            // This might break the protocol but the user wants that.
            return;
        }

        await ChannelAdapter.SendPacketAsync(packet, cancellationToken).ConfigureAwait(false);
        Statistics.HandleSentPacket(packet);
    }

    public async Task StopAsync(MqttServerClientDisconnectOptions disconnectOptions)
    {
        IsRunning = false;

        if (!_disconnectPacketSent)
        {
            // // Sending DISCONNECT packets from the server to the client is only supported when using MQTTv5+.
            if (ChannelAdapter.PacketFormatterAdapter.ProtocolVersion == MqttProtocolVersion.V500)
            {
                // From RFC: The Client or Server MAY send a DISCONNECT packet before closing the Network Connection.
                // This library does not sent a DISCONNECT packet for a normal disconnection.
                // TODO: Maybe adding a configuration option is requested in the future.
                if (disconnectOptions != null)
                {
                    if (disconnectOptions.ReasonCode != MqttDisconnectReasonCode.NormalDisconnection || disconnectOptions.UserProperties?.Any() == true ||
                        !string.IsNullOrEmpty(disconnectOptions.ReasonString) || !string.IsNullOrEmpty(disconnectOptions.ServerReference))
                    {
                        // It is very important to send the DISCONNECT packet here BEFORE cancelling the
                        // token because the entire connection is closed (disposed) as soon as the cancellation
                        // token is cancelled. To there is no chance that the DISCONNECT packet will ever arrive
                        // at the client!
                        await TrySendDisconnectPacket(disconnectOptions).ConfigureAwait(false);
                    }
                }
            }
        }

        StopInternal();
    }

    Task ClientAcknowledgedPublishPacket(MqttPublishPacket publishPacket, MqttPacketWithIdentifier acknowledgePacket)
    {
        if (_eventContainer.ClientAcknowledgedPublishPacketEvent.HasHandlers)
        {
            var eventArgs = new ClientAcknowledgedPublishPacketEventArgs(Id, Session.Items, publishPacket, acknowledgePacket);
            return _eventContainer.ClientAcknowledgedPublishPacketEvent.TryInvokeAsync(eventArgs, _logger);
        }

        return CompletedTask.Instance;
    }

    void HandleIncomingPingReqPacket()
    {
        // See: The Server MUST send a PINGRESP packet in response to a PINGREQ packet [MQTT-3.12.4-1].
        Session.EnqueueHealthPacket(new MqttPacketBusItem(MqttPingRespPacket.Instance));
    }

    Task HandleIncomingPubAckPacket(MqttPubAckPacket pubAckPacket)
    {
        var acknowledgedPublishPacket = Session.AcknowledgePublishPacket(pubAckPacket.PacketIdentifier);

        if (acknowledgedPublishPacket != null)
        {
            return ClientAcknowledgedPublishPacket(acknowledgedPublishPacket, pubAckPacket);
        }

        return CompletedTask.Instance;
    }

    Task HandleIncomingPubCompPacket(MqttPubCompPacket pubCompPacket)
    {
        var acknowledgedPublishPacket = Session.AcknowledgePublishPacket(pubCompPacket.PacketIdentifier);

        if (acknowledgedPublishPacket != null)
        {
            return ClientAcknowledgedPublishPacket(acknowledgedPublishPacket, pubCompPacket);
        }

        return CompletedTask.Instance;
    }

    async Task HandleIncomingPublishPacket(MqttPublishPacket publishPacket, CancellationToken cancellationToken)
    {
        HandleTopicAlias(publishPacket);

        var applicationMessage = MqttApplicationMessageFactory.Create(publishPacket);

        var dispatchApplicationMessageResult = await _sessionsManager.DispatchApplicationMessage(Id, Session.Items, applicationMessage, cancellationToken).ConfigureAwait(false);

        if (dispatchApplicationMessageResult.CloseConnection)
        {
            await StopAsync(new MqttServerClientDisconnectOptions { ReasonCode = MqttDisconnectReasonCode.UnspecifiedError });
            return;
        }

        switch (publishPacket.QualityOfServiceLevel)
        {
            case MqttQualityOfServiceLevel.AtMostOnce:
            {
                // Do nothing since QoS 0 has no ACK at all!
                break;
            }
            case MqttQualityOfServiceLevel.AtLeastOnce:
            {
                var pubAckPacket = MqttPubAckPacketFactory.Create(publishPacket, dispatchApplicationMessageResult);
                Session.EnqueueControlPacket(new MqttPacketBusItem(pubAckPacket));
                break;
            }
            case MqttQualityOfServiceLevel.ExactlyOnce:
            {
                var pubRecPacket = MqttPubRecPacketFactory.Create(publishPacket, dispatchApplicationMessageResult);
                Session.EnqueueControlPacket(new MqttPacketBusItem(pubRecPacket));
                break;
            }
            default:
            {
                throw new MqttCommunicationException("Received a not supported QoS level");
            }
        }
    }

    Task HandleIncomingPubRecPacket(MqttPubRecPacket pubRecPacket)
    {
        // Do not fire the event _ClientAcknowledgedPublishPacket_ here because the QoS 2 process is only finished
        // properly when the client has sent the PUBCOMP packet.
        var pubRelPacket = MqttPubRelPacketFactory.Create(pubRecPacket, MqttApplicationMessageReceivedReasonCode.Success);
        Session.EnqueueControlPacket(new MqttPacketBusItem(pubRelPacket));

        return CompletedTask.Instance;
    }

    void HandleIncomingPubRelPacket(MqttPubRelPacket pubRelPacket)
    {
        var pubCompPacket = MqttPubCompPacketFactory.Create(pubRelPacket, MqttApplicationMessageReceivedReasonCode.Success);
        Session.EnqueueControlPacket(new MqttPacketBusItem(pubCompPacket));
    }

    async Task HandleIncomingSubscribePacket(MqttSubscribePacket subscribePacket, CancellationToken cancellationToken)
    {
        var subscribeResult = await Session.Subscribe(subscribePacket, cancellationToken).ConfigureAwait(false);

        var subAckPacket = MqttSubAckPacketFactory.Create(subscribePacket, subscribeResult);

        Session.EnqueueControlPacket(new MqttPacketBusItem(subAckPacket));

        if (subscribeResult.CloseConnection)
        {
            StopInternal();
            return;
        }

        if (subscribeResult.RetainedMessages != null)
        {
            foreach (var retainedMessageMatch in subscribeResult.RetainedMessages)
            {
                var publishPacket = MqttPublishPacketFactory.Create(retainedMessageMatch);
                Session.EnqueueDataPacket(new MqttPacketBusItem(publishPacket));
            }
        }
    }

    async Task HandleIncomingUnsubscribePacket(MqttUnsubscribePacket unsubscribePacket, CancellationToken cancellationToken)
    {
        var unsubscribeResult = await Session.Unsubscribe(unsubscribePacket, cancellationToken).ConfigureAwait(false);

        var unsubAckPacket = MqttUnsubAckPacketFactory.Create(unsubscribePacket, unsubscribeResult);

        Session.EnqueueControlPacket(new MqttPacketBusItem(unsubAckPacket));

        if (unsubscribeResult.CloseConnection)
        {
            StopInternal();
        }
    }

    void HandleTopicAlias(MqttPublishPacket publishPacket)
    {
        if (publishPacket.TopicAlias == 0)
        {
            return;
        }

        lock (_topicAlias)
        {
            if (!string.IsNullOrEmpty(publishPacket.Topic))
            {
                _topicAlias[publishPacket.TopicAlias] = publishPacket.Topic;
            }
            else
            {
                if (_topicAlias.TryGetValue(publishPacket.TopicAlias, out var topic))
                {
                    publishPacket.Topic = topic;
                }
                else
                {
                    _logger.Warning("Client '{0}': Received invalid topic alias ({1})", Id, publishPacket.TopicAlias);
                }
            }
        }
    }

    async Task<MqttPacket> InterceptPacketAsync(MqttPacket packet, CancellationToken cancellationToken)
    {
        if (!_eventContainer.InterceptingOutboundPacketEvent.HasHandlers)
        {
            return packet;
        }

        var interceptingPacketEventArgs = new InterceptingPacketEventArgs(cancellationToken, Id, Endpoint, packet, Session.Items);
        await _eventContainer.InterceptingOutboundPacketEvent.InvokeAsync(interceptingPacketEventArgs).ConfigureAwait(false);

        if (!interceptingPacketEventArgs.ProcessPacket || packet == null)
        {
            return null;
        }

        return interceptingPacketEventArgs.Packet;
    }

    async Task ReceivePackagesLoop(CancellationToken cancellationToken)
    {
        MqttPacket currentPacket = null;
        try
        {
            // We do not listen for the cancellation token here because the internal buffer might still
            // contain data to be read even if the TCP connection was already dropped. So we rely on an
            // own exception in the reading loop!
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Yield();

                currentPacket = await ChannelAdapter.ReceivePacketAsync(cancellationToken).ConfigureAwait(false);
                if (currentPacket == null)
                {
                    return;
                }

                // Check for cancellation again because receive packet might block some time.
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                // The TCP connection of this client may be still open but the client has already been taken over by
                // a new TCP connection. So we must exit here to make sure to no longer process any message.
                if (IsTakenOver || !IsRunning)
                {
                    return;
                }

                var processPacket = true;

                if (_eventContainer.InterceptingInboundPacketEvent.HasHandlers)
                {
                    var interceptingPacketEventArgs = new InterceptingPacketEventArgs(cancellationToken, Id, Endpoint, currentPacket, Session.Items);
                    await _eventContainer.InterceptingInboundPacketEvent.InvokeAsync(interceptingPacketEventArgs).ConfigureAwait(false);
                    currentPacket = interceptingPacketEventArgs.Packet;
                    processPacket = interceptingPacketEventArgs.ProcessPacket;
                }

                if (!processPacket || currentPacket == null)
                {
                    // Restart the receiving process to get the next packet ignoring the current one..
                    continue;
                }

                Statistics.HandleReceivedPacket(currentPacket);

                if (currentPacket is MqttPublishPacket publishPacket)
                {
                    await HandleIncomingPublishPacket(publishPacket, cancellationToken).ConfigureAwait(false);
                }
                else if (currentPacket is MqttPubAckPacket pubAckPacket)
                {
                    await HandleIncomingPubAckPacket(pubAckPacket).ConfigureAwait(false);
                }
                else if (currentPacket is MqttPubCompPacket pubCompPacket)
                {
                    await HandleIncomingPubCompPacket(pubCompPacket).ConfigureAwait(false);
                }
                else if (currentPacket is MqttPubRecPacket pubRecPacket)
                {
                    await HandleIncomingPubRecPacket(pubRecPacket).ConfigureAwait(false);
                }
                else if (currentPacket is MqttPubRelPacket pubRelPacket)
                {
                    HandleIncomingPubRelPacket(pubRelPacket);
                }
                else if (currentPacket is MqttSubscribePacket subscribePacket)
                {
                    await HandleIncomingSubscribePacket(subscribePacket, cancellationToken).ConfigureAwait(false);
                }
                else if (currentPacket is MqttUnsubscribePacket unsubscribePacket)
                {
                    await HandleIncomingUnsubscribePacket(unsubscribePacket, cancellationToken).ConfigureAwait(false);
                }
                else if (currentPacket is MqttPingReqPacket)
                {
                    HandleIncomingPingReqPacket();
                }
                else if (currentPacket is MqttPingRespPacket)
                {
                    throw new MqttProtocolViolationException("A PINGRESP Packet is sent by the Server to the Client in response to a PINGREQ Packet only.");
                }
                else if (currentPacket is MqttDisconnectPacket disconnectPacket)
                {
                    DisconnectPacket = disconnectPacket;
                    return;
                }
                else
                {
                    throw new MqttProtocolViolationException("Packet not allowed");
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            if (exception is MqttCommunicationException)
            {
                _logger.Warning(exception, "Client '{0}': Communication exception while receiving packets", Id);
                return;
            }

            var logLevel = MqttNetLogLevel.Error;

            if (!IsRunning)
            {
                // There was an exception but the connection is already closed. So there is no chance to send a response to the client etc.
                logLevel = MqttNetLogLevel.Warning;
            }

            if (currentPacket == null)
            {
                _logger.Publish(logLevel, exception, "Client '{0}': Error while receiving packets", Id);
            }
            else
            {
                _logger.Publish(logLevel, exception, "Client '{0}': Error while processing {1} packet", Id, currentPacket.GetRfcName());
            }
        }
    }

    async Task SendPacketsLoop(CancellationToken cancellationToken)
    {
        MqttPacketBusItem packetBusItem = null;

        try
        {
            while (!cancellationToken.IsCancellationRequested && !IsTakenOver && IsRunning)
            {
                packetBusItem = await Session.DequeuePacketAsync(cancellationToken).ConfigureAwait(false);

                // Also check the cancellation token here because the dequeue is blocking and may take some time.
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                if (IsTakenOver || !IsRunning)
                {
                    return;
                }

                try
                {
                    await SendPacketAsync(packetBusItem.Packet, cancellationToken).ConfigureAwait(false);
                    packetBusItem.Complete();
                }
                catch (OperationCanceledException)
                {
                    packetBusItem.Cancel();
                }
                catch (Exception exception)
                {
                    packetBusItem.Fail(exception);
                }
                finally
                {
                    await Task.Yield();
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            if (exception is MqttCommunicationTimedOutException)
            {
                _logger.Warning(exception, "Client '{0}': Sending PUBLISH packet failed due to timeout", Id);
            }
            else if (exception is MqttCommunicationException)
            {
                _logger.Warning(exception, "Client '{0}': Sending PUBLISH packet failed due to communication exception", Id);
            }
            else
            {
                _logger.Error(exception, "Client '{0}': Sending PUBLISH packet failed", Id);
            }

            if (packetBusItem?.Packet is MqttPublishPacket publishPacket)
            {
                if (publishPacket.QualityOfServiceLevel > MqttQualityOfServiceLevel.AtMostOnce)
                {
                    publishPacket.Dup = true;
                    Session.EnqueueDataPacket(new MqttPacketBusItem(publishPacket));
                }
            }

            StopInternal();
        }
    }

    void StopInternal()
    {
        _cancellationToken?.TryCancel();
    }

    async Task TrySendDisconnectPacket(MqttServerClientDisconnectOptions options)
    {
        try
        {
            // This also indicates that it was tried at least!
            _disconnectPacketSent = true;

            var disconnectPacket = MqttDisconnectPacketFactory.Create(options);

            using (var timeout = new CancellationTokenSource(_serverOptions.DefaultCommunicationTimeout))
            {
                await SendPacketAsync(disconnectPacket, timeout.Token).ConfigureAwait(false);
            }
        }
        catch (Exception exception)
        {
            _logger.Warning(exception, "Client '{0}': Error while sending DISCONNECT packet (ReasonCode = {1})", Id, options.ReasonCode);
        }
    }
}