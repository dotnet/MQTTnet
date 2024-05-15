// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace MQTTnet.Client;

public sealed class MqttClientConnectingEventArgs : EventArgs
{
    public MqttClientConnectingEventArgs(MqttClientOptions clientOptions)
    {
        ClientOptions = clientOptions;
    }

    public MqttClientOptions ClientOptions { get; }
}