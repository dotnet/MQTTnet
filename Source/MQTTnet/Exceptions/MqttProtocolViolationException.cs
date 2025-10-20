// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace MQTTnet.Exceptions;

public class MqttProtocolViolationException(string message) : Exception(message)
{
    public const uint VariableByteIntegerMaxValue = 268435455;

    public static void ThrowIfVariableByteIntegerExceedsLimit(uint value)
    {
        if (value > VariableByteIntegerMaxValue)
        {
            throw new MqttProtocolViolationException($"The value {value} is too large for a variable byte integer ({VariableByteIntegerMaxValue}).");
        }
    }
}