using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MQTTnet.Server.Configuration;

namespace MQTTnet.Server.Mqtt
{
    public class MqttServerStorage : IMqttServerStorage
    {
        private readonly SettingsModel _settings;

        public MqttServerStorage(SettingsModel settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public Task SaveRetainedMessagesAsync(IList<MqttApplicationMessage> messages)
        {
            return Task.CompletedTask;
        }

        public Task<IList<MqttApplicationMessage>> LoadRetainedMessagesAsync()
        {
            return Task.FromResult<IList<MqttApplicationMessage>>(null);
        }
    }
}
