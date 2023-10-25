// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace MQTTnet.Client
{
    public static class MqttClientTcpOptionsExtensions
    {
        public static int GetPort(this MqttClientTcpOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            if (options.Port.HasValue)
            {
                return options.Port.Value;
            }

            return !(options.TlsOptions?.UseTls ?? false) ? 1883 : 8883;
        }
    }
}
