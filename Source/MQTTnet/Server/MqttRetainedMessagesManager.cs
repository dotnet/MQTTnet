using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MQTTnet.Diagnostics;
using MQTTnet.Internal;

namespace MQTTnet.Server
{
    public class MqttRetainedMessagesManager : IMqttRetainedMessagesManager
    {
        private readonly byte[] _emptyArray = new byte[0];
        private readonly AsyncLock _messagesLock = new AsyncLock();
        private readonly Dictionary<string, MqttApplicationMessage> _messages = new Dictionary<string, MqttApplicationMessage>();

        private IMqttNetChildLogger _logger;
        private IMqttServerOptions _options;

        public Task Start(IMqttServerOptions options, IMqttNetChildLogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            _logger = logger.CreateChildLogger(nameof(MqttRetainedMessagesManager));
            _options = options ?? throw new ArgumentNullException(nameof(options));
#if NET452
            return Task.FromResult(0);
#else
            return Task.CompletedTask;
#endif
        }

        public async Task LoadMessagesAsync()
        {
            if (_options.Storage == null)
            {
                return;
            }

            try
            {
                var retainedMessages = await _options.Storage.LoadRetainedMessagesAsync().ConfigureAwait(false);
                if (retainedMessages?.Any() == true)
                {
                    using (await _messagesLock.WaitAsync().ConfigureAwait(false))
                    {
                        _messages.Clear();

                        foreach (var retainedMessage in retainedMessages)
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

        public async Task HandleMessageAsync(string clientId, MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));

            try
            {
                using (await _messagesLock.WaitAsync().ConfigureAwait(false))
                {
                    var saveIsRequired = false;
                    var hasPayload = applicationMessage.Payload != null && applicationMessage.Payload.Length > 0;

                    if (!hasPayload)
                    {
                        saveIsRequired = _messages.Remove(applicationMessage.Topic);
                        _logger.Verbose("Client '{0}' cleared retained message for topic '{1}'.", clientId, applicationMessage.Topic);
                    }
                    else
                    {
                        if (!_messages.TryGetValue(applicationMessage.Topic, out var existingMessage))
                        {
                            _messages[applicationMessage.Topic] = applicationMessage;
                            saveIsRequired = true;
                        }
                        else
                        {
                            if (existingMessage.QualityOfServiceLevel != applicationMessage.QualityOfServiceLevel || !existingMessage.Payload.SequenceEqual(applicationMessage.Payload ?? _emptyArray))
                            {
                                _messages[applicationMessage.Topic] = applicationMessage;
                                saveIsRequired = true;
                            }
                        }

                        _logger.Verbose("Client '{0}' set retained message for topic '{1}'.", clientId, applicationMessage.Topic);
                    }

                    if (saveIsRequired)
                    {
                        if (_options.Storage != null)
                        {
                            var messagesForSave = new List<MqttApplicationMessage>(_messages.Values);
                            await _options.Storage.SaveRetainedMessagesAsync(messagesForSave).ConfigureAwait(false);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Unhandled exception while handling retained messages.");
            }
        }

        public async Task<IList<MqttApplicationMessage>> GetSubscribedMessagesAsync(ICollection<TopicFilter> topicFilters)
        {
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            var matchingRetainedMessages = new List<MqttApplicationMessage>();

            List<MqttApplicationMessage> retainedMessages;
            using (await _messagesLock.WaitAsync().ConfigureAwait(false))
            {
                retainedMessages = _messages.Values.ToList();
            }

            foreach (var retainedMessage in retainedMessages)
            {
                foreach (var topicFilter in topicFilters)
                {
                    if (!MqttTopicFilterComparer.IsMatch(retainedMessage.Topic, topicFilter.Topic))
                    {
                        continue;
                    }

                    matchingRetainedMessages.Add(retainedMessage);
                    break;
                }
            }

            return matchingRetainedMessages;
        }

        public async Task<IList<MqttApplicationMessage>> GetMessagesAsync()
        {
            using (await _messagesLock.WaitAsync().ConfigureAwait(false))
            {
                return _messages.Values.ToList();
            }
        }

        public async Task ClearMessagesAsync()
        {
            using (await _messagesLock.WaitAsync().ConfigureAwait(false))
            {
                _messages.Clear();

                if (_options.Storage != null)
                {
                    await _options.Storage.SaveRetainedMessagesAsync(new List<MqttApplicationMessage>()).ConfigureAwait(false);
                }
            }
        }
    }
}
