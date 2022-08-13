// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Diagnostics;
using MQTTnet.Implementations;
using MQTTnet.Internal;

namespace MQTTnet.Server
{
    public sealed class MqttRetainedMessagesManager
    {
        readonly MqttServerEventContainer _eventContainer;
        readonly MqttNetSourceLogger _logger;
        readonly ConcurrentDictionary<string, MqttRetainedMessage> _messages = new ConcurrentDictionary<string, MqttRetainedMessage>();
        readonly AsyncLock _storageAccessLock = new AsyncLock();
        
        CancellationTokenSource _cancellationToken;

        public MqttRetainedMessagesManager(MqttServerEventContainer eventContainer, IMqttNetLogger logger)
        {
            _eventContainer = eventContainer ?? throw new ArgumentNullException(nameof(eventContainer));

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _logger = logger.WithSource(nameof(MqttRetainedMessagesManager));
        }

        public async Task ClearMessages()
        {
            _messages.Clear();

            using (await _storageAccessLock.WaitAsync(CancellationToken.None).ConfigureAwait(false))
            {
                await _eventContainer.RetainedMessagesClearedEvent.InvokeAsync(EventArgs.Empty).ConfigureAwait(false);
            }
        }

        public Task<IList<MqttApplicationMessage>> GetMessages()
        {
            var result = new List<MqttApplicationMessage>(_messages.Select(m => m.Value.ApplicationMessage));
            return Task.FromResult((IList<MqttApplicationMessage>)result);
        }

        public async Task HandleMessage(string clientId, MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null)
            {
                throw new ArgumentNullException(nameof(applicationMessage));
            }

            try
            {
                List<MqttRetainedMessage> allMessages = null;
                var saveIsRequired = false;

                var hasPayload = applicationMessage.Payload != null && applicationMessage.Payload.Length > 0;
                var changeType = RetainedMessageChangeType.Created;

                var retainedMessage = new MqttRetainedMessage
                {
                    ApplicationMessage = applicationMessage,
                    PublishedTimestamp = DateTime.UtcNow
                };

                MqttRetainedMessage changedMessage;

                if (!hasPayload)
                {
                    // Messages without a payload will remove the message.
                    changeType = RetainedMessageChangeType.Deleted;
                    saveIsRequired = _messages.TryRemove(applicationMessage.Topic, out changedMessage);
                    _logger.Verbose("Client '{0}' cleared retained message for topic '{1}'.", clientId, applicationMessage.Topic);
                }
                else
                {
                    var messageChanged = false;

                    _messages.AddOrUpdate(
                        applicationMessage.Topic,
                        _ =>
                        {
                            changeType = RetainedMessageChangeType.Created;
                            changedMessage = retainedMessage;
                            saveIsRequired = true;

                            return retainedMessage;
                        },
                        (_, existingMessage) =>
                        {
                            var hasSamePayload = existingMessage.ApplicationMessage.Payload.SequenceEqual(applicationMessage.Payload ?? PlatformAbstractionLayer.EmptyByteArray);
                            var hasSameQoSLevel = existingMessage.ApplicationMessage.QualityOfServiceLevel == applicationMessage.QualityOfServiceLevel;

                            if (!hasSameQoSLevel || !hasSamePayload)
                            {
                                changeType = RetainedMessageChangeType.Updated;
                                saveIsRequired = true;
                                messageChanged = true;
                                return retainedMessage;
                            }

                            // Keep the existing message.
                            return existingMessage;
                        });

                    if (messageChanged)
                    {
                        _logger.Verbose("Client '{0}' set retained message for topic '{1}'.", clientId, applicationMessage.Topic);
                    }
                }

                if (saveIsRequired)
                {
                    allMessages = new List<MqttRetainedMessage>(_messages.Values);
                }

                if (saveIsRequired)
                {
                    await SaveMessages(clientId, retainedMessage, changeType, allMessages).ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Error while handling retained messages.");
            }
        }

        public async Task Start()
        {
            _cancellationToken = new CancellationTokenSource();

            await LoadMessages().ConfigureAwait(false);

            _ = Task.Run(() => RemoveExpiredMessagesLoop(_cancellationToken.Token), _cancellationToken.Token);
        }

        public Task Stop()
        {
            _cancellationToken?.Cancel();
            _cancellationToken?.Dispose();
            _cancellationToken = null;

            return PlatformAbstractionLayer.CompletedTask;
        }

        async Task LoadMessages()
        {
            if (!_eventContainer.LoadingRetainedMessagesEvent.HasHandlers)
            {
                return;
            }

            var eventArgs = new LoadingRetainedMessagesEventArgs();
            await _eventContainer.LoadingRetainedMessagesEvent.InvokeAsync(eventArgs).ConfigureAwait(false);

            _messages.Clear();

            if (eventArgs.LoadedRetainedMessages != null)
            {
                foreach (var retainedMessage in eventArgs.LoadedRetainedMessages)
                {
                    _messages[retainedMessage.ApplicationMessage.Topic] = retainedMessage;
                }
            }
        }

        async Task RemoveExpiredMessages()
        {
            var expiredMessages = new List<MqttRetainedMessage>();

            var now = DateTime.UtcNow;

            foreach (var retainedMessage in _messages)
            {
                if (retainedMessage.Value.ApplicationMessage.MessageExpiryInterval == 0)
                {
                    // The feature is not used.
                    continue;
                }

                var messageAge = (int)(now - retainedMessage.Value.PublishedTimestamp).TotalSeconds;
                if (messageAge < retainedMessage.Value.ApplicationMessage.MessageExpiryInterval)
                {
                    // The message is not expired yet.
                    continue;
                }

                expiredMessages.Add(retainedMessage.Value);
            }

            foreach (var expiredMessage in expiredMessages)
            {
                if (_messages.TryRemove(expiredMessage.ApplicationMessage.Topic, out _))
                {
                    _logger.Verbose("Server cleared retained message for topic '{0}' due to expiration.", expiredMessage.ApplicationMessage.Topic);

                    await SaveMessages(null, expiredMessage, RetainedMessageChangeType.Deleted, new List<MqttRetainedMessage>(_messages.Values));
                }
            }
        }

        async Task RemoveExpiredMessagesLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await RemoveExpiredMessages().ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, "Error while removing expired retained messages.");
                }
                finally
                {
                    // The expiry interval is specified in seconds so that a message may gets expired 
                    // every second.
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
                }
            }
        }

        async Task SaveMessages(string clientId, MqttRetainedMessage changedMessage, RetainedMessageChangeType changeType, List<MqttRetainedMessage> allMessages)
        {
            using (await _storageAccessLock.WaitAsync(CancellationToken.None).ConfigureAwait(false))
            {
                if (_eventContainer.RetainedMessageChangedEvent.HasHandlers)
                {
                    var eventArgs = new RetainedMessageChangedEventArgs(clientId, changedMessage, changeType, allMessages);
                    await _eventContainer.RetainedMessageChangedEvent.InvokeAsync(eventArgs).ConfigureAwait(false);
                }
            }
        }
    }
}