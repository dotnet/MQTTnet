// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace MQTTnet;

public static class MqttApplicationMessageExtensions
{
    public static string ConvertPayloadToString(this MqttApplicationMessage applicationMessage)
    {
        ArgumentNullException.ThrowIfNull(applicationMessage);

        if (applicationMessage.Payload.Length == 0)
        {
            return null;
        }

        return Encoding.UTF8.GetString(applicationMessage.Payload);
    }

    public static TValue ConvertPayloadToJson<TValue>(this MqttApplicationMessage applicationMessage, JsonTypeInfo<TValue> jsonTypeInfo)
    {
        ArgumentNullException.ThrowIfNull(applicationMessage);

        var jsonReader = new Utf8JsonReader(applicationMessage.Payload);
        return JsonSerializer.Deserialize(ref jsonReader, jsonTypeInfo);
    }

    public static TValue ConvertPayloadToJson<TValue>(this MqttApplicationMessage applicationMessage, JsonSerializerOptions jsonSerializerOptions = null)
    {
        ArgumentNullException.ThrowIfNull(applicationMessage);

        var jsonReader = new Utf8JsonReader(applicationMessage.Payload);
        return JsonSerializer.Deserialize<TValue>(ref jsonReader, jsonSerializerOptions);
    }
}