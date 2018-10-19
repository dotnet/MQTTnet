using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MQTTnet.Diagnostics;

namespace MQTTnet.Server
{
    public class MqttRetainedMessagesManager
    {
        private readonly Dictionary<string, MqttApplicationMessage> _messages = new Dictionary<string, MqttApplicationMessage>();

        private readonly IMqttNetChildLogger _logger;
        private readonly IMqttServerOptions _options;

        public MqttRetainedMessagesManager(IMqttServerOptions options, IMqttNetChildLogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            _logger = logger.CreateChildLogger(nameof(MqttRetainedMessagesManager));
            _options = options ?? throw new ArgumentNullException(nameof(options));
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

                lock (_messages)
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
                List<MqttApplicationMessage> messagesForSave = null;
                lock (_messages)
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
                            if (existingMessage.QualityOfServiceLevel != applicationMessage.QualityOfServiceLevel || !existingMessage.Payload.SequenceEqual(applicationMessage.Payload ?? new byte[0]))
                            {
                                _messages[applicationMessage.Topic] = applicationMessage;
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

                if (messagesForSave == null)
                {
                    _logger.Verbose("Skipped saving retained messages because no changes were detected.");
                    return;
                }

                if (_options.Storage != null)
                {
                    await _options.Storage.SaveRetainedMessagesAsync(messagesForSave).ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Unhandled exception while handling retained messages.");
            }
        }

        public IList<MqttApplicationMessage> GetSubscribedMessages(ICollection<TopicFilter> topicFilters)
        {
            var retainedMessages = new List<MqttApplicationMessage>();

            lock (_messages)
            {
                foreach (var retainedMessage in _messages.Values)
                {
                    foreach (var topicFilter in topicFilters)
                    {
                        if (!MqttTopicFilterComparer.IsMatch(retainedMessage.Topic, topicFilter.Topic))
                        {
                            continue;
                        }

                        retainedMessages.Add(retainedMessage);
                        break;
                    }
                }
            }

            return retainedMessages;
        }

        public IList<MqttApplicationMessage> GetMessages()
        {
            lock (_messages)
            {
                return _messages.Values.ToList();
            }
        }

        public Task ClearMessagesAsync()
        {
            lock (_messages)
            {
                _messages.Clear();
            }

            if (_options.Storage != null)
            {
                return _options.Storage.SaveRetainedMessagesAsync(new List<MqttApplicationMessage>());
            }

            return Task.FromResult((object)null);
        }
    }
}
