// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Adapter;
using MQTTnet.Diagnostics.Logger;

namespace MQTTnet.AspNetCore
{
    sealed class AspNetCoreMqttClientFactory : MqttClientFactory, IMqttClientFactory
    {
        public AspNetCoreMqttClientFactory(
            IMqttNetLogger logger,
            IMqttClientAdapterFactory clientAdapterFactory) : base(logger, clientAdapterFactory)
        {
        }
    }
}
