// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace MQTTnet.Exceptions;

public sealed class MqttClientDisconnectedException : MqttCommunicationException
{
    public MqttClientDisconnectedException(Exception innerException) : base("The MQTT client is disconnected.", innerException)
    {
    }
}