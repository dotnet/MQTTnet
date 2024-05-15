// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using MQTTnet.Client;
using MQTTnet.Exceptions;

namespace MQTTnet.Adapter;

public sealed class MqttConnectingFailedException : MqttCommunicationException
{
    public MqttConnectingFailedException(string message, Exception innerException, MqttClientConnectResult connectResult) : base(message, innerException)
    {
        Result = connectResult;
    }

    public MqttClientConnectResult Result { get; }

    public MqttClientConnectResultCode ResultCode => Result?.ResultCode ?? MqttClientConnectResultCode.UnspecifiedError;
}