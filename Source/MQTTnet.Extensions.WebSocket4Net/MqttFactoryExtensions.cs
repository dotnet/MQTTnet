// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace MQTTnet.Extensions.WebSocket4Net
{
    public static class MqttFactoryExtensions
    {
        public static MqttFactory UseWebSocket4Net(this MqttFactory mqttFactory)
        {
            if (mqttFactory == null) throw new ArgumentNullException(nameof(mqttFactory));

            return mqttFactory.UseClientAdapterFactory(new WebSocket4NetMqttClientAdapterFactory());
        }
    }
}
