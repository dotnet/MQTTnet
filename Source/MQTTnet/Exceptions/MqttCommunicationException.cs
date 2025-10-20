// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace MQTTnet.Exceptions;

public class MqttCommunicationException : Exception
{
    public MqttCommunicationException(Exception? innerException)
        : base(innerException?.Message ?? "MQTT communication failed.", innerException)
    {
    }

    public MqttCommunicationException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}