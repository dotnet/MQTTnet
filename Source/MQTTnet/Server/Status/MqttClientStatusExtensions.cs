// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using MQTTnet.Protocol;
using MQTTnet.Server.Disconnecting;

namespace MQTTnet.Server
{
    public static class MqttClientStatusExtensions
    {
        static readonly MqttServerClientDisconnectOptions DefaultDisconnectOptions = new MqttServerClientDisconnectOptions
        {
            ReasonCode = MqttDisconnectReasonCode.NormalDisconnection,
            ReasonString = null,
            UserProperties = null,
            ServerReference = null
        };

        public static Task DisconnectAsync(this MqttClientStatus clientStatus)
        {
            if (clientStatus == null)
            {
                throw new ArgumentNullException(nameof(clientStatus));
            }

            return clientStatus.DisconnectAsync(DefaultDisconnectOptions);
        }
    }
}