// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MQTTnet.Client;
using MQTTnet.Packets;

namespace MQTTnet.Extensions.ManagedClient
{
    public sealed class SubscribeProcessedEventArgs : EventArgs
    {
        public SubscribeProcessedEventArgs(MqttClientSubscribeOptions options, MqttClientSubscribeResultCode result, IReadOnlyCollection<MqttUserProperty> userProperties)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
            Result = result;
            UserProperties = userProperties;
        }

        public MqttClientSubscribeOptions Options { get; }

        public MqttClientSubscribeResultCode Result { get; }

        public IReadOnlyCollection<MqttUserProperty> UserProperties { get; }
    }
}