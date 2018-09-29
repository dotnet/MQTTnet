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
                await HandleMessageInternalAsync(clientId, applicationMessage).ConfigureAwait(false);
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

        public Task ClearMessagesAsync()
        {
            lock (_messages)
            {
                _messages.Clear();
            }

            return _options.Storage.SaveRetainedMessagesAsync(new List<MqttApplicationMessage>());
        }

        private async Task HandleMessageInternalAsync(string clientId, MqttApplicationMessage applicationMessage)
        {
            var saveIsRequired = false;

            lock (_messages)
            {
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
            }

            if (!saveIsRequired)
            {
                _logger.Verbose("Skipped saving retained messages because no changes were detected.");
            }

            if (saveIsRequired && _options.Storage != null)
            {
                List<MqttApplicationMessage> messages;
                lock (_messages)
                {
                    messages = _messages.Values.ToList();
                }

                await _options.Storage.SaveRetainedMessagesAsync(messages).ConfigureAwait(false);
            }
        }
    }
}
