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

        return applicationMessage.Payload.IsEmpty
            ? null
            : Encoding.UTF8.GetString(applicationMessage.Payload);
    }

    public static TValue ConvertPayloadToJson<TValue>(this MqttApplicationMessage applicationMessage, JsonTypeInfo<TValue> jsonTypeInfo)
    {
        ArgumentNullException.ThrowIfNull(applicationMessage);
        ArgumentNullException.ThrowIfNull(jsonTypeInfo);

        var readerOptions = CreateJsonReaderOptions(jsonTypeInfo.Options);
        var jsonReader = new Utf8JsonReader(applicationMessage.Payload, readerOptions);
        return JsonSerializer.Deserialize(ref jsonReader, jsonTypeInfo);
    }

    public static TValue ConvertPayloadToJson<TValue>(this MqttApplicationMessage applicationMessage, JsonSerializerOptions jsonSerializerOptions = null)
    {
        ArgumentNullException.ThrowIfNull(applicationMessage);

        var readerOptions = CreateJsonReaderOptions(jsonSerializerOptions);
        var jsonReader = new Utf8JsonReader(applicationMessage.Payload, readerOptions);
        return JsonSerializer.Deserialize<TValue>(ref jsonReader, jsonSerializerOptions);
    }

    private static JsonReaderOptions CreateJsonReaderOptions(JsonSerializerOptions jsonSerializerOptions)
    {
        jsonSerializerOptions ??= JsonSerializerOptions.Default;
        return new JsonReaderOptions
        {
            MaxDepth = jsonSerializerOptions.MaxDepth,
            AllowTrailingCommas = jsonSerializerOptions.AllowTrailingCommas,
            CommentHandling = jsonSerializerOptions.ReadCommentHandling
        };
    }
}