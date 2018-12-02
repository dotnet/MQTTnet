using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Exceptions;
using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public class MqttClientSessionsManager : IDisposable
    {
        private readonly BlockingCollection<MqttEnqueuedApplicationMessage> _messageQueue = new BlockingCollection<MqttEnqueuedApplicationMessage>();

        /// <summary>
        /// manual locking dictionaries is faster than using concurrent dictionary
        /// </summary>
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
            Task.Factory.StartNew(() => TryProcessQueuedApplicationMessages(_cancellationToken), _cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public void Stop()
        {
            lock (_sessions)
            {
                foreach (var session in _sessions)
                {
                    session.Value.Stop(MqttClientDisconnectType.NotClean);
                }

                _sessions.Clear();
            }
        }

        public Task StartSession(IMqttChannelAdapter clientAdapter)
        {
            return Task.Run(() => RunSessionAsync(clientAdapter, _cancellationToken), _cancellationToken);
        }

        public IList<IMqttClientSessionStatus> GetClientStatus()
        {
            var result = new List<IMqttClientSessionStatus>();

            foreach (var session in GetSessions())
            {
                var status = new MqttClientSessionStatus(this, session);
                session.FillStatus(status);

                result.Add(status);
            }

            return result;
        }

        public void EnqueueApplicationMessage(MqttClientSession senderClientSession, MqttPublishPacket publishPacket)
        {
            if (publishPacket == null) throw new ArgumentNullException(nameof(publishPacket));

            _messageQueue.Add(new MqttEnqueuedApplicationMessage(senderClientSession, publishPacket), _cancellationToken);
        }

        public Task SubscribeAsync(string clientId, IList<TopicFilter> topicFilters)
        {
            if (clientId == null) throw new ArgumentNullException(nameof(clientId));
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            lock (_sessions)
            {
                if (!_sessions.TryGetValue(clientId, out var session))
                {
                    throw new InvalidOperationException($"Client session '{clientId}' is unknown.");
                }

                return session.SubscribeAsync(topicFilters);
            }
        }

        public Task UnsubscribeAsync(string clientId, IList<string> topicFilters)
        {
            if (clientId == null) throw new ArgumentNullException(nameof(clientId));
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            lock (_sessions)
            {
                if (!_sessions.TryGetValue(clientId, out var session))
                {
                    throw new InvalidOperationException($"Client session '{clientId}' is unknown.");
                }

                return session.UnsubscribeAsync(topicFilters);
            }
        }

        public void DeleteSession(string clientId)
        {
            lock (_sessions)
            {
                _sessions.Remove(clientId);
            }

            _logger.Verbose("Session for client '{0}' deleted.", clientId);
        }

        public void Dispose()
        {
            _messageQueue?.Dispose();
        }

        private void TryProcessQueuedApplicationMessages(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    TryProcessNextQueuedApplicationMessage(cancellationToken);
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

        private void TryProcessNextQueuedApplicationMessage(CancellationToken cancellationToken)
        {
            try
            {
                var enqueuedApplicationMessage = _messageQueue.Take(cancellationToken);
                var sender = enqueuedApplicationMessage.Sender;
                var applicationMessage = enqueuedApplicationMessage.PublishPacket.ToApplicationMessage();

                var interceptorContext = InterceptApplicationMessage(sender, applicationMessage);
                if (interceptorContext != null)
                {
                    if (interceptorContext.CloseConnection)
                    {
                        enqueuedApplicationMessage.Sender.Stop(MqttClientDisconnectType.NotClean);
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
                    _retainedMessagesManager.HandleMessageAsync(sender?.ClientId, applicationMessage).GetAwaiter().GetResult();
                }

                foreach (var clientSession in GetSessions())
                {
                    var publishPacket = applicationMessage.ToPublishPacket();

                    // Set the retain flag to true according to [MQTT-3.3.1-9].
                    publishPacket.Retain = false;

                    clientSession.EnqueueApplicationMessage(enqueuedApplicationMessage.Sender, publishPacket);
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

        private List<MqttClientSession> GetSessions()
        {
            lock (_sessions)
            {
                return _sessions.Values.ToList();
            }
        }

        private async Task RunSessionAsync(IMqttChannelAdapter clientAdapter, CancellationToken cancellationToken)
        {
            var clientId = string.Empty;
            
            try
            {
                var firstPacket = await clientAdapter.ReceivePacketAsync(_options.DefaultCommunicationTimeout, cancellationToken).ConfigureAwait(false);
                if (firstPacket == null)
                {
                    return;
                }

                if (!(firstPacket is MqttConnectPacket connectPacket))
                {
                    throw new MqttProtocolViolationException("The first packet from a client must be a 'CONNECT' packet [MQTT-3.1.0-1].");
                }

                clientId = connectPacket.ClientId;

                // Switch to the required protocol version before sending any response.
                clientAdapter.PacketSerializer.ProtocolVersion = connectPacket.ProtocolVersion;

                var connectReturnCode = ValidateConnection(connectPacket, clientAdapter);
                if (connectReturnCode != MqttConnectReturnCode.ConnectionAccepted)
                {
                    await clientAdapter.SendPacketAsync(
                        new MqttConnAckPacket
                        {
                            ConnectReturnCode = connectReturnCode
                        },
                        cancellationToken).ConfigureAwait(false);

                    return;
                }

                var result = PrepareClientSession(connectPacket);

                await clientAdapter.SendPacketAsync(
                    new MqttConnAckPacket
                    {
                        ConnectReturnCode = connectReturnCode,
                        IsSessionPresent = result.IsExistingSession
                    },
                    cancellationToken).ConfigureAwait(false);

                _logger.Info("Client '{0}': Connected.", clientId);
                _eventDispatcher.OnClientConnected(clientId);

                await result.Session.RunAsync(connectPacket, clientAdapter).ConfigureAwait(false);
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
                if (!_options.EnablePersistentSessions)
                {
                    DeleteSession(clientId);
                }
            }
        }

        private MqttConnectReturnCode ValidateConnection(MqttConnectPacket connectPacket, IMqttChannelAdapter clientAdapter)
        {
            if (_options.ConnectionValidator == null)
            {
                return MqttConnectReturnCode.ConnectionAccepted;
            }

            var context = new MqttConnectionValidatorContext(
                connectPacket.ClientId,
                connectPacket.Username,
                connectPacket.Password,
                connectPacket.WillMessage,
                clientAdapter.Endpoint);

            _options.ConnectionValidator(context);
            return context.ReturnCode;
        }

        private PrepareClientSessionResult PrepareClientSession(MqttConnectPacket connectPacket)
        {
            lock (_sessions)
            {
                var isSessionPresent = _sessions.TryGetValue(connectPacket.ClientId, out var clientSession);
                if (isSessionPresent)
                {
                    if (connectPacket.CleanSession)
                    {
                        _sessions.Remove(connectPacket.ClientId);

                        clientSession.Stop(MqttClientDisconnectType.Clean);
                        clientSession.Dispose();
                        clientSession = null;

                        _logger.Verbose("Stopped existing session of client '{0}'.", connectPacket.ClientId);
                    }
                    else
                    {
                        _logger.Verbose("Reusing existing session of client '{0}'.", connectPacket.ClientId);
                    }
                }

                var isExistingSession = true;
                if (clientSession == null)
                {
                    isExistingSession = false;

                    clientSession = new MqttClientSession(connectPacket.ClientId, _options, this, _retainedMessagesManager, _eventDispatcher, _logger);
                    _sessions[connectPacket.ClientId] = clientSession;

                    _logger.Verbose("Created a new session for client '{0}'.", connectPacket.ClientId);
                }

                return new PrepareClientSessionResult { IsExistingSession = isExistingSession, Session = clientSession };
            }
        }

        private MqttApplicationMessageInterceptorContext InterceptApplicationMessage(MqttClientSession sender, MqttApplicationMessage applicationMessage)
        {
            var interceptor = _options.ApplicationMessageInterceptor;
            if (interceptor == null)
            {
                return null;
            }

            var interceptorContext = new MqttApplicationMessageInterceptorContext(sender?.ClientId, applicationMessage);
            interceptor(interceptorContext);
            return interceptorContext;
        }
    }
}