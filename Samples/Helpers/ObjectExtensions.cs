// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json;

namespace MQTTnet.Samples.Helpers;

internal static class ObjectExtensions
{
    static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public static TObject DumpToConsole<TObject>(this TObject @object)
    {
        var output = "NULL";
        if (@object != null)
        {
            output = JsonSerializer.Serialize(@object, SerializerOptions);
        }

        Console.WriteLine($"[{@object?.GetType().Name}]:\r\n{output}");
        return @object;
    }
}