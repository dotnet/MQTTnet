// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Connections;

namespace MQTTnet.AspNetCore;

public static class ConnectionBuilderExtensions
{
    public static IConnectionBuilder UseMqtt(this IConnectionBuilder builder)
    {
        return builder.UseConnectionHandler<MqttConnectionHandler>();
    }
}