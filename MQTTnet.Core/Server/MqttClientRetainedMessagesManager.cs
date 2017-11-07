using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MQTTnet.Core.Packets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MQTTnet.Core.Server
{
    public sealed class MqttClientRetainedMessagesManager : IMqttClientRetainedMessageManager
    {
        private readonly Dictionary<string, MqttApplicationMessage> _retainedMessages = new Dictionary<string, MqttApplicationMessage>();
        private readonly ILogger<MqttClientRetainedMessagesManager> _logger;
        private readonly MqttServerOptions _options;

        public MqttClientRetainedMessagesManager(IOptions<MqttServerOptions> options, ILogger<MqttClientRetainedMessagesManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task LoadMessagesAsync()
        {
            if (_options.Storage == null)
            {
                return;
            }

            try
            {
                var retainedMessages = await _options.Storage.LoadRetainedMessagesAsync();
                lock (_retainedMessages)
                {
                    _retainedMessages.Clear();
                    foreach (var retainedMessage in retainedMessages)
                    {
                        _retainedMessages[retainedMessage.Topic] = retainedMessage;
                    }
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(new EventId(), exception, "Unhandled exception while loading retained messages.");
            }
        }

        public async Task HandleMessageAsync(string clientId, MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));

            List<MqttApplicationMessage> allRetainedMessages;
            lock (_retainedMessages)
            {
                if (applicationMessage.Payload?.Any() == false)
                {
                    _retainedMessages.Remove(applicationMessage.Topic);
                    _logger.LogInformation("Client '{0}' cleared retained message for topic '{1}'.", clientId, applicationMessage.Topic);
                }
                else
                {
                    _retainedMessages[applicationMessage.Topic] = applicationMessage;
                    _logger.LogInformation("Client '{0}' updated retained message for topic '{1}'.", clientId, applicationMessage.Topic);
                }

                allRetainedMessages = new List<MqttApplicationMessage>(_retainedMessages.Values);
            }

            try
            {
                if (_options.Storage != null)
                {
                    await _options.Storage.SaveRetainedMessagesAsync(allRetainedMessages);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(new EventId(), exception, "Unhandled exception while saving retained messages.");
            }
        }

        public List<MqttApplicationMessage> GetMessages(MqttSubscribePacket subscribePacket)
        {
            var retainedMessages = new List<MqttApplicationMessage>();
            lock (_retainedMessages)
            {
                foreach (var retainedMessage in _retainedMessages.Values)
                {
                    foreach (var topicFilter in subscribePacket.TopicFilters)
                    {
                        if (retainedMessage.QualityOfServiceLevel < topicFilter.QualityOfServiceLevel)
                        {
                            continue;
                        }

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
    }
}
