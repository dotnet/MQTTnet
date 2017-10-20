using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MQTTnet.Core.Diagnostics;
using MQTTnet.Core.Internal;
using MQTTnet.Core.Packets;

namespace MQTTnet.Core.Server
{
    public sealed class MqttClientRetainedMessagesManager
    {
        private readonly MqttNetTrace _trace;
        private readonly Dictionary<string, MqttPublishPacket> _retainedMessages = new Dictionary<string, MqttPublishPacket>();
        private readonly MqttServerOptions _options;

        public MqttClientRetainedMessagesManager(MqttServerOptions options, MqttNetTrace trace)
        {
            _trace = trace ?? throw new ArgumentNullException(nameof(trace));
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
                var retainedMessages = await _options.Storage.LoadRetainedMessagesAsync();
                lock (_retainedMessages)
                {
                    _retainedMessages.Clear();
                    foreach (var retainedMessage in retainedMessages)
                    {
                        _retainedMessages[retainedMessage.Topic] = retainedMessage.ToPublishPacket();
                    }
                }
            }
            catch (Exception exception)
            {
                _trace.Error(nameof(MqttClientRetainedMessagesManager), exception, "Unhandled exception while loading retained messages.");
            }
        }

        public async Task HandleMessageAsync(string clientId, MqttPublishPacket publishPacket)
        {
            if (publishPacket == null) throw new ArgumentNullException(nameof(publishPacket));

            List<MqttPublishPacket> allRetainedMessages;
            lock (_retainedMessages)
            {
                if (publishPacket.Payload?.Any() == false)
                {
                    _retainedMessages.Remove(publishPacket.Topic);
                    _trace.Information(nameof(MqttClientRetainedMessagesManager), "Client '{0}' cleared retained message for topic '{1}'.", clientId, publishPacket.Topic);
                }
                else
                {
                    _retainedMessages[publishPacket.Topic] = publishPacket;
                    _trace.Information(nameof(MqttClientRetainedMessagesManager), "Client '{0}' updated retained message for topic '{1}'.", clientId, publishPacket.Topic);
                }

                allRetainedMessages = new List<MqttPublishPacket>(_retainedMessages.Values);
            }

            try
            {
                // ReSharper disable once UseNullPropagation
                if (_options.Storage != null)
                {
                    await _options.Storage.SaveRetainedMessagesAsync(allRetainedMessages.Select(p => p.ToApplicationMessage()).ToList());
                }
            }
            catch (Exception exception)
            {
                _trace.Error(nameof(MqttClientRetainedMessagesManager), exception, "Unhandled exception while saving retained messages.");
            }
        }

        public List<MqttPublishPacket> GetMessages(MqttSubscribePacket subscribePacket)
        {
            var retainedMessages = new List<MqttPublishPacket>();
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
