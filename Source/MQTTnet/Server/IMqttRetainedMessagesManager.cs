﻿using MQTTnet.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;
using MQTTnet.Diagnostics.Logger;

namespace MQTTnet.Server
{
    public interface IMqttRetainedMessagesManager
    {
        Task Start(IMqttServerOptions options, IMqttNetLogger logger);

        Task LoadMessagesAsync();

        Task ClearMessagesAsync();

        Task HandleMessageAsync(string clientId, MqttApplicationMessage applicationMessage);

        Task<IList<MqttApplicationMessage>> GetMessagesAsync();
    }
}
