// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Buffers;
using MQTTnet.Diagnostics;
using MQTTnet.Internal;
using System.Buffers;

namespace MQTTnet.Server.Internal
{
    public sealed class MqttRetainedMessagesManager
    {
        readonly Dictionary<string, MqttApplicationMessage> _messages = new Dictionary<string, MqttApplicationMessage>(4096);
        readonly AsyncLock _storageAccessLock = new AsyncLock();

        readonly MqttServerEventContainer _eventContainer;
        readonly MqttNetSourceLogger _logger;

        public MqttRetainedMessagesManager(MqttServerEventContainer eventContainer, IMqttNetLogger logger)
        {
            _eventContainer = eventContainer ?? throw new ArgumentNullException(nameof(eventContainer));

            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            _logger = logger.WithSource(nameof(MqttRetainedMessagesManager));
        }

        public async Task Start()
        {
            try
            {
                var eventArgs = new LoadingRetainedMessagesEventArgs();
                await _eventContainer.LoadingRetainedMessagesEvent.InvokeAsync(eventArgs).ConfigureAwait(false);

                lock (_messages)
                {
                    _messages.Clear();

                    if (eventArgs.LoadedRetainedMessages != null)
                    {
                        foreach (var retainedMessage in eventArgs.LoadedRetainedMessages)
                        {
                            _messages[retainedMessage.Topic] = retainedMessage;
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Unhandled exception while loading retained messages.");
            }
        }

        public async Task UpdateMessage(string clientId, MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null)
            {
                throw new ArgumentNullException(nameof(applicationMessage));
            }

            try
            {
                List<MqttApplicationMessage> messagesForSave = null;
                var saveIsRequired = false;

                lock (_messages)
                {
                    var payload = applicationMessage.Payload;
                    var hasPayload = payload.Length > 0;

                    if (!hasPayload)
                    {
                        saveIsRequired = _messages.Remove(applicationMessage.Topic);
                        _logger.Verbose("Client '{0}' cleared retained message for topic '{1}'.", clientId, applicationMessage.Topic);
                    }
                    else
                    {
                        if (!_messages.TryGetValue(applicationMessage.Topic, out var existingMessage))
                        {
                            _messages[applicationMessage.Topic] = applicationMessage.Clone();
                            saveIsRequired = true;
                        }
                        else
                        {
                            if (existingMessage.QualityOfServiceLevel != applicationMessage.QualityOfServiceLevel ||
                                !MqttMemoryHelper.SequenceEqual(existingMessage.Payload.Sequence, payload.Sequence))
                            {
                                _messages[applicationMessage.Topic] = applicationMessage.Clone();
                                saveIsRequired = true;
                            }
                        }

                        _logger.Verbose("Client '{0}' set retained message for topic '{1}'.", clientId, applicationMessage.Topic);
                    }

                    if (saveIsRequired)
                    {
                        messagesForSave = new List<MqttApplicationMessage>(_messages.Values);
                    }
                }

                if (saveIsRequired)
                {
                    using (await _storageAccessLock.EnterAsync().ConfigureAwait(false))
                    {
                        var eventArgs = new RetainedMessageChangedEventArgs(clientId, applicationMessage, messagesForSave);
                        await _eventContainer.RetainedMessageChangedEvent.InvokeAsync(eventArgs).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Unhandled exception while handling retained messages.");
            }
        }

        public Task<IList<MqttApplicationMessage>> GetMessages()
        {
            lock (_messages)
            {
                var result = new List<MqttApplicationMessage>(_messages.Values);
                return Task.FromResult((IList<MqttApplicationMessage>)result);
            }
        }

        public Task<MqttApplicationMessage> GetMessage(string topic)
        {
            lock (_messages)
            {
                if (_messages.TryGetValue(topic, out var message))
                {
                    return Task.FromResult(message);
                }
            }

            return Task.FromResult<MqttApplicationMessage>(null);
        }

        public async Task ClearMessages()
        {
            lock (_messages)
            {
                _messages.Clear();
            }

            using (await _storageAccessLock.EnterAsync().ConfigureAwait(false))
            {
                await _eventContainer.RetainedMessagesClearedEvent.InvokeAsync(EventArgs.Empty).ConfigureAwait(false);
            }
        }
    }
}