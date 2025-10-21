// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Exceptions;

namespace MQTTnet.Adapter;

public sealed class MqttConnectingFailedException : MqttCommunicationException
{
    public MqttConnectingFailedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}