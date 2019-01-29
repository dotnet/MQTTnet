using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MQTTnet.Diagnostics;
using MQTTnet.Internal;

namespace MQTTnet.Server
{
    public class MqttRetainedMessagesManager
    {
        private readonly byte[] _emptyArray = new byte[0];
        private readonly AsyncLock _messagesLock = new AsyncLock();
        private readonly Dictionary<string, MqttApplicationMessage> _messages = new Dictionary<string, MqttApplicationMessage>();

        private readonly IMqttNetChildLogger _logger;
        private readonly IMqttServerOptions _options;
        private readonly IMqttServerStorage _storage;

        public MqttRetainedMessagesManager(IMqttServerOptions options, IMqttServerStorage storage, IMqttNetChildLogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            _logger = logger.CreateChildLogger(nameof(MqttRetainedMessagesManager));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            if (_options.Storage != null)
            {
                _storage = _options.Storage;
            }
            else
            {
                _storage = storage;
            }
        }

        public async Task LoadMessagesAsync()
        {
            if (_storage == null)
            {
                return;
            }

            try
            {
                var retainedMessages = await _storage.LoadRetainedMessagesAsync().ConfigureAwait(false);

                using (await _messagesLock.WaitAsync().ConfigureAwait(false))
                {
                    _messages.Clear();
                    foreach (var retainedMessage in retainedMessages)
                    {
                        _messages[retainedMessage.Topic] = retainedMessage;
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

                    if (applicationMessage.Payload?.Length == 0)
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
                        if (_storage != null)
                        {
                            var messagesForSave = new List<MqttApplicationMessage>(_messages.Values);
                            await _storage.SaveRetainedMessagesAsync(messagesForSave).ConfigureAwait(false);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Unhandled exception while handling retained messages.");
            }
        }

        public async Task<List<MqttApplicationMessage>> GetSubscribedMessagesAsync(ICollection<TopicFilter> topicFilters)
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

                if (_storage != null)
                {
                    await _storage.SaveRetainedMessagesAsync(new List<MqttApplicationMessage>()).ConfigureAwait(false);
                }
            }
        }
    }
}
