// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Adapter;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using MQTTnet.Implementations;
using MQTTnet.Internal;
using MQTTnet.PacketDispatcher;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public sealed class MqttClient
    {
        readonly MqttApplicationMessageFactory _applicationMessageFactory = new MqttApplicationMessageFactory();
        readonly MqttConnectPacket _connectPacket;
        readonly MqttServerEventContainer _eventContainer;
        readonly MqttNetSourceLogger _logger;
        readonly MqttPacketDispatcher _packetDispatcher = new MqttPacketDispatcher();
        readonly MqttPacketFactories _packetFactories = new MqttPacketFactories();

        readonly MqttServerOptions _serverOptions;

        readonly MqttClientSessionsManager _sessionsManager;
        readonly Dictionary<ushort, string> _topicAlias = new Dictionary<ushort, string>();

        CancellationTokenSource _cancellationToken;

        public MqttClient(
            MqttConnectPacket connectPacket,
            IMqttChannelAdapter channelAdapter,
            MqttSession session,
            MqttServerOptions serverOptions,
            MqttServerEventContainer eventContainer,
            MqttClientSessionsManager sessionsManager,
            IMqttNetLogger logger)
        {
            _serverOptions = serverOptions ?? throw new ArgumentNullException(nameof(serverOptions));
            _eventContainer = eventContainer;
            _sessionsManager = sessionsManager ?? throw new ArgumentNullException(nameof(sessionsManager));

            ChannelAdapter = channelAdapter ?? throw new ArgumentNullException(nameof(channelAdapter));
            Endpoint = channelAdapter.Endpoint;

            Session = session ?? throw new ArgumentNullException(nameof(session));
            _connectPacket = connectPacket ?? throw new ArgumentNullException(nameof(connectPacket));

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _logger = logger.WithSource(nameof(MqttClient));
        }

        public IMqttChannelAdapter ChannelAdapter { get; }

        public string Endpoint { get; }

        public string Id => _connectPacket.ClientId;

        public bool IsCleanDisconnect { get; private set; }

        public bool IsRunning { get; private set; }

        public bool IsTakenOver { get; set; }

        public ushort KeepAlivePeriod => _connectPacket.KeepAlivePeriod;

        public MqttSession Session { get; }

        public MqttClientStatistics Statistics { get; } = new MqttClientStatistics();

        public void ResetStatistics()
        {
            ChannelAdapter.ResetStatistics();
        }

        public async Task RunAsync()
        {
            _logger.Info("Client '{0}': Session started.", Id);

            Session.LatestConnectPacket = _connectPacket;
            Session.WillMessageSent = false;

            using (_cancellationToken = new CancellationTokenSource())
            {
                var cancellationToken = _cancellationToken.Token;

                try
                {
                    Task.Run(() => SendPacketsLoop(cancellationToken), cancellationToken).RunInBackground(_logger);

                    IsRunning = true;

                    await ReceivePackagesLoop(cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    IsRunning = false;

                    _cancellationToken?.Cancel();
                    _cancellationToken = null;
                }
            }

            _packetDispatcher.CancelAll();

            if (!IsTakenOver && !IsCleanDisconnect && Session.LatestConnectPacket.WillFlag && !Session.WillMessageSent)
            {
                var willPublishPacket = _packetFactories.Publish.Create(Session.LatestConnectPacket);
                var willApplicationMessage = _applicationMessageFactory.Create(willPublishPacket);

                _ = _sessionsManager.DispatchApplicationMessage(Id, willApplicationMessage);
                Session.WillMessageSent = true;

                _logger.Info("Client '{0}': Published will message.", Id);
            }

            _logger.Info("Client '{0}': Connection stopped.", Id);
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

        public async Task StopAsync(MqttDisconnectReasonCode reason)
        {
            IsRunning = false;

            // Potentially send disconnect reason to MQTT 5 clients

            if (ChannelAdapter.PacketFormatterAdapter.ProtocolVersion == MqttProtocolVersion.V500 && reason != MqttDisconnectReasonCode.NormalDisconnection)            
            {
                // Is is very important to send the DISCONNECT packet here BEFORE cancelling the
                // token because the entire connection is closed (disposed) as soon as the cancellation
                // token is cancelled. To there is no chance that the DISCONNECT packet will ever arrive
                // at the client!
                await TrySendDisconnectPacket(reason).ConfigureAwait(false);
            }

            StopInternal();
        }

        Task ClientAcknowledgedPublishPacket(MqttPublishPacket publishPacket, MqttPacketWithIdentifier acknowledgePacket)
        {
            if (_eventContainer.ClientAcknowledgedPublishPacketEvent.HasHandlers)
            {
                var eventArgs = new ClientAcknowledgedPublishPacketEventArgs
                {
                    PublishPacket = publishPacket,
                    AcknowledgePacket = acknowledgePacket,
                    ClientId = Id,
                    SessionItems = Session.Items
                };

                return _eventContainer.ClientAcknowledgedPublishPacketEvent.TryInvokeAsync(eventArgs, _logger);
            }

            return PlatformAbstractionLayer.CompletedTask;
        }

        Task HandleIncomingPubAckPacket(MqttPubAckPacket pubAckPacket)
        {
            var acknowledgedPublishPacket = Session.AcknowledgePublishPacket(pubAckPacket.PacketIdentifier);

            if (acknowledgedPublishPacket != null)
            {
                return ClientAcknowledgedPublishPacket(acknowledgedPublishPacket, pubAckPacket);
            }

            return PlatformAbstractionLayer.CompletedTask;
        }

        Task HandleIncomingPubCompPacket(MqttPubCompPacket pubCompPacket)
        {
            var acknowledgedPublishPacket = Session.AcknowledgePublishPacket(pubCompPacket.PacketIdentifier);

            if (acknowledgedPublishPacket != null)
            {
                return ClientAcknowledgedPublishPacket(acknowledgedPublishPacket, pubCompPacket);
            }

            return PlatformAbstractionLayer.CompletedTask;
        }

        async Task HandleIncomingPublishPacket(MqttPublishPacket publishPacket, CancellationToken cancellationToken)
        {
            HandleTopicAlias(publishPacket);

            InterceptingPublishEventArgs interceptingPublishEventArgs = null;
            var applicationMessage = _applicationMessageFactory.Create(publishPacket);
            var closeConnection = false;
            var processPublish = true;

            if (_eventContainer.InterceptingPublishEvent.HasHandlers)
            {
                interceptingPublishEventArgs = new InterceptingPublishEventArgs(applicationMessage, cancellationToken, Id, Session.Items);
                if (string.IsNullOrEmpty(interceptingPublishEventArgs.ApplicationMessage.Topic))
                {
                    // This can happen if a topic alias us used but the topic is
                    // unknown to the server.
                    interceptingPublishEventArgs.Response.ReasonCode = MqttPubAckReasonCode.TopicNameInvalid;
                    interceptingPublishEventArgs.ProcessPublish = false;
                }

                await _eventContainer.InterceptingPublishEvent.InvokeAsync(interceptingPublishEventArgs).ConfigureAwait(false);

                applicationMessage = interceptingPublishEventArgs.ApplicationMessage;
                closeConnection = interceptingPublishEventArgs.CloseConnection;
                processPublish = interceptingPublishEventArgs.ProcessPublish;
            }

            if (closeConnection)
            {
                await StopAsync(MqttDisconnectReasonCode.UnspecifiedError);
                return;
            }

            if (processPublish && applicationMessage != null)
            {
                await _sessionsManager.DispatchApplicationMessage(Id, applicationMessage).ConfigureAwait(false);
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
                    var pubAckPacket = _packetFactories.PubAck.Create(publishPacket, interceptingPublishEventArgs);
                    Session.EnqueueControlPacket(new MqttPacketBusItem(pubAckPacket));
                    break;
                }
                case MqttQualityOfServiceLevel.ExactlyOnce:
                {
                    var pubRecPacket = _packetFactories.PubRec.Create(publishPacket, interceptingPublishEventArgs);
                    Session.EnqueueControlPacket(new MqttPacketBusItem(pubRecPacket));
                    break;
                }
                default:
                {
                    throw new MqttCommunicationException("Received a not supported QoS level.");
                }
            }
        }

        async Task HandleIncomingPubRecPacket(MqttPubRecPacket pubRecPacket)
        {
            var acknowledgedPublishPacket = Session.PeekAcknowledgePublishPacket(pubRecPacket.PacketIdentifier);

            if (acknowledgedPublishPacket != null)
            {
                await ClientAcknowledgedPublishPacket(acknowledgedPublishPacket, pubRecPacket).ConfigureAwait(false);
            }

            var pubRelPacket = _packetFactories.PubRel.Create(pubRecPacket, MqttApplicationMessageReceivedReasonCode.Success);
            Session.EnqueueControlPacket(new MqttPacketBusItem(pubRelPacket));
        }

        void HandleIncomingPubRelPacket(MqttPubRelPacket pubRelPacket)
        {
            var pubCompPacket = _packetFactories.PubComp.Create(pubRelPacket, MqttApplicationMessageReceivedReasonCode.Success);
            Session.EnqueueControlPacket(new MqttPacketBusItem(pubCompPacket));
        }

        async Task HandleIncomingSubscribePacket(MqttSubscribePacket subscribePacket, CancellationToken cancellationToken)
        {
            var subscribeResult = await Session.SubscriptionsManager.Subscribe(subscribePacket, cancellationToken).ConfigureAwait(false);

            var subAckPacket = _packetFactories.SubAck.Create(subscribePacket, subscribeResult);

            Session.EnqueueControlPacket(new MqttPacketBusItem(subAckPacket));

            if (subscribeResult.CloseConnection)
            {
                StopInternal();
                return;
            }

            if (subscribeResult.RetainedMessages != null)
            {
                foreach (var retainedApplicationMessage in subscribeResult.RetainedMessages)
                {
                    var publishPacket = _packetFactories.Publish.Create(retainedApplicationMessage.ApplicationMessage);
                    Session.EnqueueDataPacket(new MqttPacketBusItem(publishPacket));
                }
            }
        }


        async Task HandleIncomingUnsubscribePacket(MqttUnsubscribePacket unsubscribePacket, CancellationToken cancellationToken)
        {
            var unsubscribeResult = await Session.SubscriptionsManager.Unsubscribe(unsubscribePacket, cancellationToken).ConfigureAwait(false);

            var unsubAckPacket = _packetFactories.UnsubAck.Create(unsubscribePacket, unsubscribeResult);

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
                        _logger.Warning("Client '{0}': Received invalid topic alias ({1}).", Id, publishPacket.TopicAlias);
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
            try
            {
                // We do not listen for the cancellation token here because the internal buffer might still
                // contain data to be read even if the TCP connection was already dropped. So we rely on an
                // own exception in the reading loop!
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Yield();

                    var packet = await ChannelAdapter.ReceivePacketAsync(cancellationToken).ConfigureAwait(false);
                    if (packet == null)
                    {
                        return;
                    }

                    var processPacket = true;

                    if (_eventContainer.InterceptingInboundPacketEvent.HasHandlers)
                    {
                        var interceptingPacketEventArgs = new InterceptingPacketEventArgs(cancellationToken, Id, Endpoint, packet, Session.Items);
                        await _eventContainer.InterceptingInboundPacketEvent.InvokeAsync(interceptingPacketEventArgs).ConfigureAwait(false);
                        packet = interceptingPacketEventArgs.Packet;
                        processPacket = interceptingPacketEventArgs.ProcessPacket;
                    }

                    if (!processPacket || packet == null)
                    {
                        // Restart the receiving process to get the next packet ignoring the current one..
                        continue;
                    }

                    Statistics.HandleReceivedPacket(packet);

                    if (packet is MqttPublishPacket publishPacket)
                    {
                        await HandleIncomingPublishPacket(publishPacket, cancellationToken).ConfigureAwait(false);
                    }
                    else if (packet is MqttPubAckPacket pubAckPacket)
                    {
                        await HandleIncomingPubAckPacket(pubAckPacket).ConfigureAwait(false);
                    }
                    else if (packet is MqttPubCompPacket pubCompPacket)
                    {
                        await HandleIncomingPubCompPacket(pubCompPacket).ConfigureAwait(false);
                    }
                    else if (packet is MqttPubRecPacket pubRecPacket)
                    {
                        await HandleIncomingPubRecPacket(pubRecPacket).ConfigureAwait(false);
                    }
                    else if (packet is MqttPubRelPacket pubRelPacket)
                    {
                        HandleIncomingPubRelPacket(pubRelPacket);
                    }
                    else if (packet is MqttSubscribePacket subscribePacket)
                    {
                        await HandleIncomingSubscribePacket(subscribePacket, cancellationToken).ConfigureAwait(false);
                    }
                    else if (packet is MqttUnsubscribePacket unsubscribePacket)
                    {
                        await HandleIncomingUnsubscribePacket(unsubscribePacket, cancellationToken).ConfigureAwait(false);
                    }
                    else if (packet is MqttPingReqPacket)
                    {
                        // See: The Server MUST send a PINGRESP packet in response to a PINGREQ packet [MQTT-3.12.4-1].
                        Session.EnqueueHealthPacket(new MqttPacketBusItem(MqttPingRespPacket.Instance));
                    }
                    else if (packet is MqttPingRespPacket)
                    {
                        throw new MqttProtocolViolationException("A PINGRESP Packet is sent by the Server to the Client in response to a PINGREQ Packet only.");
                    }
                    else if (packet is MqttDisconnectPacket)
                    {
                        IsCleanDisconnect = true;
                        return;
                    }
                    else
                    {
                        if (!_packetDispatcher.TryDispatch(packet))
                        {
                            throw new MqttProtocolViolationException($"Received packet '{packet}' at an unexpected time.");
                        }
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
                    _logger.Warning(exception, "Client '{0}': Communication exception while receiving client packets.", Id);
                }
                else
                {
                    _logger.Error(exception, "Client '{0}': Error while receiving client packets.", Id);
                }
            }
        }

        async Task SendPacketsLoop(CancellationToken cancellationToken)
        {
            MqttPacketBusItem packetBusItem = null;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    packetBusItem = await Session.DequeuePacketAsync(cancellationToken).ConfigureAwait(false);

                    // Also check the cancellation token here because the dequeue is blocking and may take some time.
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    try
                    {
                        await SendPacketAsync(packetBusItem.Packet, cancellationToken).ConfigureAwait(false);
                        packetBusItem.MarkAsDelivered();
                    }
                    catch (OperationCanceledException)
                    {
                        packetBusItem.MarkAsCancelled();
                    }
                    catch (Exception exception)
                    {
                        packetBusItem.MarkAsFailed(exception);
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
                    _logger.Warning(exception, "Client '{0}': Sending publish packet failed: Timeout.", Id);
                }
                else if (exception is MqttCommunicationException)
                {
                    _logger.Warning(exception, "Client '{0}': Sending publish packet failed: Communication exception.", Id);
                }
                else
                {
                    _logger.Error(exception, "Client '{0}': Sending publish packet failed.", Id);
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
            try
            {
                _cancellationToken?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // This can happen when connections are created and dropped very quickly.
                // It is not an issue if the cancellation token cannot be cancelled multiple times.
            }
        }

        async Task TrySendDisconnectPacket(MqttDisconnectReasonCode reasonCode)
        {
            try
            {
                var disconnectPacket = _packetFactories.Disconnect.Create(reasonCode);

                using (var timeout = new CancellationTokenSource(_serverOptions.DefaultCommunicationTimeout))
                {
                    await SendPacketAsync(disconnectPacket, timeout.Token).ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                _logger.Warning(exception, "Client '{0}': Error while sending DISCONNECT packet (ReasonCode = {1}).", Id, reasonCode);
            }
        }
    }
}