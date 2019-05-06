﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet.Server.Configuration;
using Newtonsoft.Json;

namespace MQTTnet.Server.Mqtt
{
    public class MqttServerStorage : IMqttServerStorage
    {
        private readonly List<MqttApplicationMessage> _messages = new List<MqttApplicationMessage>();

        private readonly SettingsModel _settings;
        private readonly ILogger<MqttServerStorage> _logger;

        private string _filename;
        private bool _messagesHaveChanged;

        public MqttServerStorage(SettingsModel settings, ILogger<MqttServerStorage> logger)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Configure()
        {
            if (_settings.RetainedApplicationMessages?.Persist != true)
            {
                _logger.LogInformation("Persisting of retained application messages is disabled.");
                return;
            }

            _filename = ExpandFilename();

            // The retained application messages are stored in a separate thread.
            // This is mandatory because writing them to a slow storage (like RaspberryPi SD card) 
            // will slow down the whole message processing speed.
            Task.Run(SaveRetainedMessagesInternalAsync, CancellationToken.None);
        }

        public Task SaveRetainedMessagesAsync(IList<MqttApplicationMessage> messages)
        {
            lock (_messages)
            {
                _messages.Clear();
                _messages.AddRange(messages);

                _messagesHaveChanged = true;
            }

            return Task.CompletedTask;
        }

        private async Task SaveRetainedMessagesInternalAsync()
        {
            while (true)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(_settings.RetainedApplicationMessages.WriteInterval)).ConfigureAwait(false);

                    List<MqttApplicationMessage> messages;
                    lock (_messages)
                    {
                        if (!_messagesHaveChanged)
                        {
                            continue;
                        }

                        messages = new List<MqttApplicationMessage>(_messages);
                        _messagesHaveChanged = false;
                    }

                    var json = JsonConvert.SerializeObject(messages);
                    await File.WriteAllTextAsync(_filename, json, Encoding.UTF8).ConfigureAwait(false);
                    
                    _logger.LogInformation($"{messages.Count} retained MQTT messages written.");
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Error while writing retained MQTT messages.");
                }
            }
        }

        public async Task<IList<MqttApplicationMessage>> LoadRetainedMessagesAsync()
        {
            if (_settings.RetainedApplicationMessages?.Persist != true)
            {
                return null;
            }

            if (!File.Exists(_filename))
            {
                return null;
            }

            try
            {
                var json = await File.ReadAllTextAsync(_filename).ConfigureAwait(false);
                var applicationMessages = JsonConvert.DeserializeObject<List<MqttApplicationMessage>>(json);

                _logger.LogInformation($"{applicationMessages.Count} retained MQTT messages loaded.");

                return applicationMessages;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Error while loading persisted retained application messages.");
                return null;
            }
        }

        private string ExpandFilename()
        {
            var filename = _settings.RetainedApplicationMessages.Filename;

            var uri = new Uri(filename, UriKind.RelativeOrAbsolute);
            if (!uri.IsAbsoluteUri)
            {
                filename = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filename);
            }

            return filename;
        }
    }
}
