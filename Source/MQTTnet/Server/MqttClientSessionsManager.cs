using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public class MqttClientSessionsManager : IDisposable
    {
        private readonly BlockingCollection<MqttEnqueuedApplicationMessage> _messageQueue = new BlockingCollection<MqttEnqueuedApplicationMessage>();

        private readonly AsyncLock _sessionsLock = new AsyncLock();
        private readonly Dictionary<string, MqttClientSession> _sessions = new Dictionary<string, MqttClientSession>();

        private readonly CancellationToken _cancellationToken;
        private readonly MqttServerEventDispatcher _eventDispatcher;

        private readonly MqttRetainedMessagesManager _retainedMessagesManager;
        private readonly IMqttServerOptions _options;
        private readonly IMqttNetChildLogger _logger;

        public MqttClientSessionsManager(
            IMqttServerOptions options, 
            MqttRetainedMessagesManager retainedMessagesManager, 
            CancellationToken cancellationToken,
            MqttServerEventDispatcher eventDispatcher,
            IMqttNetChildLogger logger)
        {
            _cancellationToken = cancellationToken;

            if (logger == null) throw new ArgumentNullException(nameof(logger));
            _logger = logger.CreateChildLogger(nameof(MqttClientSessionsManager));

            _eventDispatcher = eventDispatcher ?? throw new ArgumentNullException(nameof(eventDispatcher));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _retainedMessagesManager = retainedMessagesManager ?? throw new ArgumentNullException(nameof(retainedMessagesManager));
        }

        public void Start()
        {
            Task.Factory.StartNew(() => TryProcessQueuedApplicationMessagesAsync(_cancellationToken), _cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public async Task StopAsync()
        {
            List<MqttClientSession> sessions;
            using (await _sessionsLock.WaitAsync(CancellationToken.None).ConfigureAwait(false))
            {
                sessions = _sessions.Values.ToList();
            }

            foreach (var session in sessions)
            {
                await session.StopAsync(MqttClientDisconnectType.Clean).ConfigureAwait(false);
            }
        }

        public Task HandleConnectionAsync(IMqttChannelAdapter clientAdapter, MqttServerEventDispatcher dispatcher)
        {
            return HandleConnectionAsync(clientAdapter, dispatcher, _cancellationToken);

            // TODO: Check if Task.Run is required.
            //return Task.Run(() => HandleConnectionAsync(clientAdapter, _cancellationToken), _cancellationToken);
        }

        public async Task<IList<IMqttClientSessionStatus>> GetClientStatusAsync()
        {
            var result = new List<IMqttClientSessionStatus>();

            using (await _sessionsLock.WaitAsync(CancellationToken.None).ConfigureAwait(false))
            {
                foreach (var session in _sessions.Values)
                {
                    var status = new MqttClientSessionStatus(this, session);
                    session.FillStatus(status);

                    result.Add(status);
                }
            }
            
            return result;
        }

        public void EnqueueApplicationMessage(MqttClientSession senderClientSession, MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));

            _messageQueue.Add(new MqttEnqueuedApplicationMessage(senderClientSession, applicationMessage), _cancellationToken);
        }

        public async Task SubscribeAsync(string clientId, IEnumerable<TopicFilter> topicFilters)
        {
            if (clientId == null) throw new ArgumentNullException(nameof(clientId));
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            using (await _sessionsLock.WaitAsync(CancellationToken.None).ConfigureAwait(false))
            {
                if (!_sessions.TryGetValue(clientId, out var session))
                {
                    throw new InvalidOperationException($"Client session '{clientId}' is unknown.");
                }

                await session.SubscribeAsync(topicFilters).ConfigureAwait(false);
            }
        }

        public async Task UnsubscribeAsync(string clientId, IEnumerable<string> topicFilters)
        {
            if (clientId == null) throw new ArgumentNullException(nameof(clientId));
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            using (await _sessionsLock.WaitAsync(CancellationToken.None).ConfigureAwait(false))
            {
                if (!_sessions.TryGetValue(clientId, out var session))
                {
                    throw new InvalidOperationException($"Client session '{clientId}' is unknown.");
                }

                await session.UnsubscribeAsync(topicFilters).ConfigureAwait(false);
            }
        }

        public async Task DeleteSessionAsync(string clientId)
        {
            using (await _sessionsLock.WaitAsync(CancellationToken.None).ConfigureAwait(false))
            {
                if (_sessions.TryGetValue(clientId, out var session))
                {
                    session.Dispose();
                }

                _sessions.Remove(clientId);
            }

            _logger.Verbose("Session for client '{0}' deleted.", clientId);
        }

        public void Dispose()
        {
            _messageQueue?.Dispose();
        }

        private async Task TryProcessQueuedApplicationMessagesAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await TryProcessNextQueuedApplicationMessageAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, "Unhandled exception while processing queued application messages.");
                }
            }
        }

        private async Task TryProcessNextQueuedApplicationMessageAsync(CancellationToken cancellationToken)
        {
            try
            {
                var enqueuedApplicationMessage = _messageQueue.Take(cancellationToken);

                var sender = enqueuedApplicationMessage.Sender;
                var applicationMessage = enqueuedApplicationMessage.ApplicationMessage;

                var interceptorContext = await InterceptApplicationMessageAsync(sender, applicationMessage).ConfigureAwait(false);
                if (interceptorContext != null)
                {
                    if (interceptorContext.CloseConnection)
                    {
                        await enqueuedApplicationMessage.Sender.StopAsync(MqttClientDisconnectType.NotClean).ConfigureAwait(false);
                    }

                    if (interceptorContext.ApplicationMessage == null || !interceptorContext.AcceptPublish)
                    {
                        return;
                    }

                    applicationMessage = interceptorContext.ApplicationMessage;
                }

                _eventDispatcher.OnApplicationMessageReceived(sender?.ClientId, applicationMessage);

                if (applicationMessage.Retain)
                {
                    await _retainedMessagesManager.HandleMessageAsync(sender?.ClientId, applicationMessage).ConfigureAwait(false);
                }

                using (await _sessionsLock.WaitAsync(CancellationToken.None).ConfigureAwait(false))
                {
                    foreach (var clientSession in _sessions.Values)
                    {
                        await clientSession.EnqueueApplicationMessageAsync(
                            enqueuedApplicationMessage.Sender,
                            enqueuedApplicationMessage.ApplicationMessage,
                            false).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Unhandled exception while processing next queued application message.");
            }
        }

        private async Task HandleConnectionAsync(IMqttChannelAdapter channelAdapter, MqttServerEventDispatcher dispatcher, CancellationToken cancellationToken)
        {
            var clientId = string.Empty;

            try
            {
                var firstPacket = await channelAdapter.ReceivePacketAsync(_options.DefaultCommunicationTimeout, cancellationToken).ConfigureAwait(false);
                if (!(firstPacket is MqttConnectPacket connectPacket))
                {
                    _logger.Warning(null, "The first packet from client '{0}' was no 'CONNECT' packet [MQTT-3.1.0-1].", channelAdapter.Endpoint);
                    return;
                }

                clientId = connectPacket.ClientId;
                var connectReturnCode = await ValidateConnectionAsync(connectPacket, channelAdapter, dispatcher).ConfigureAwait(false);
                if (connectReturnCode != MqttConnectReturnCode.ConnectionAccepted)
                {
                    await channelAdapter.SendPacketAsync(
                        new MqttConnAckPacket
                        {
                            ReturnCode = connectReturnCode,
                            ReasonCode = MqttConnectReasonCode.NotAuthorized
                        },
                        cancellationToken).ConfigureAwait(false);

                    return;
                }

                var session = await PrepareClientSessionAsync(connectPacket).ConfigureAwait(false);
                await session.RunAsync(connectPacket, channelAdapter).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _logger.Error(exception, exception.Message);
            }
            finally
            {
                await TryCleanupChannelAsync(channelAdapter).ConfigureAwait(false);

                if (!_options.EnablePersistentSessions)
                {
                    // TODO: Check if the session will be used later.
                    // Consider reference counter or "Recycle" property
                    // Or add timer (will be required for MQTTv5 (session life time) "IsActiveProperty".
                    //öö
                    //await DeleteSessionAsync(clientId).ConfigureAwait(false);
                }
            }
        }

        private async Task<MqttConnectReturnCode> ValidateConnectionAsync(MqttConnectPacket connectPacket, IMqttChannelAdapter clientAdapter, MqttServerEventDispatcher dispatcher)
        {
            MqttConnectReturnCode returnCode = MqttConnectReturnCode.ConnectionAccepted;
            var context = new MqttConnectionValidatorContext(
                  connectPacket.ClientId,
                  connectPacket.Username,
                  connectPacket.Password,
                  connectPacket.WillMessage,
                  clientAdapter.Endpoint);
            if (_options.ConnectionValidator != null)
            {
                await _options.ConnectionValidator.ValidateConnection(context).ConfigureAwait(false);
            }
            else
            {
               dispatcher.OnClientConnectionValidatorAsync(context);
            }
            returnCode = context.ReturnCode;
            return returnCode;
        }

        private async Task<MqttClientSession> PrepareClientSessionAsync(MqttConnectPacket connectPacket)
        {
            using (await _sessionsLock.WaitAsync(CancellationToken.None).ConfigureAwait(false))
            {
                var isSessionPresent = _sessions.TryGetValue(connectPacket.ClientId, out var clientSession);
                if (isSessionPresent)
                {
                    if (connectPacket.CleanSession)
                    {
                        await clientSession.StopAsync(MqttClientDisconnectType.Clean).ConfigureAwait(false);

                        clientSession.Dispose();
                        clientSession = null;

                        _logger.Verbose("Stopped existing session of client '{0}'.", connectPacket.ClientId);
                    }
                    else
                    {
                        await clientSession.StopAsync(MqttClientDisconnectType.Clean).ConfigureAwait(false);

                        _logger.Verbose("Reusing existing session of client '{0}'.", connectPacket.ClientId);
                    }
                }

                if (clientSession == null)
                {
                    clientSession = new MqttClientSession(connectPacket.ClientId, _options, this, _retainedMessagesManager, _eventDispatcher, _logger);
                    _sessions[connectPacket.ClientId] = clientSession;

                    _logger.Verbose("Created a new session for client '{0}'.", connectPacket.ClientId);
                }

                return clientSession;
            }
        }

        private async Task<MqttApplicationMessageInterceptorContext> InterceptApplicationMessageAsync(MqttClientSession sender, MqttApplicationMessage applicationMessage)
        {
            var interceptor = _options.ApplicationMessageInterceptor;
            if (interceptor == null)
            {
                return null;
            }

            var interceptorContext = new MqttApplicationMessageInterceptorContext(sender?.ClientId, applicationMessage);
            await interceptor.InterceptApplicationMessagePublishAsync(interceptorContext).ConfigureAwait(false);
            return interceptorContext;
        }

        private async Task TryCleanupChannelAsync(IMqttChannelAdapter channelAdapter)
        {
            try
            {
                await channelAdapter.DisconnectAsync(_options.DefaultCommunicationTimeout, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Error while disconnecting client channel.");
            }
            finally
            {
                channelAdapter.Dispose();
            }
        }
    }
}