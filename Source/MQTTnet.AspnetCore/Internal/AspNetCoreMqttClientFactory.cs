// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Adapter;
using MQTTnet.Diagnostics.Logger;

namespace MQTTnet.AspNetCore
{
    sealed class AspNetCoreMqttClientFactory : IMqttClientFactory
    {
        private readonly IMqttClientAdapterFactory _mqttClientAdapterFactory;
        private readonly IMqttNetLogger _logger;

        public AspNetCoreMqttClientFactory(
            IMqttClientAdapterFactory mqttClientAdapterFactory,
            IMqttNetLogger logger)
        {
            _mqttClientAdapterFactory = mqttClientAdapterFactory;
            _logger = logger;
        }

        public IMqttClient CreateMqttClient()
        {
            return new MqttClient(_mqttClientAdapterFactory, _logger);
        }
    }
}
