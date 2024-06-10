// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;

namespace MQTTnet;

public static class MqttApplicationMessageExtensions
{
    public static string ConvertPayloadToString(this MqttApplicationMessage applicationMessage)
    {
        if (applicationMessage == null)
        {
            throw new ArgumentNullException(nameof(applicationMessage));
        }

        if (applicationMessage.Payload.Length == 0)
        {
            return null;
        }

        return Encoding.UTF8.GetString(applicationMessage.Payload);
    }
}