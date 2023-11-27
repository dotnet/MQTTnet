// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming
// ReSharper disable EmptyConstructor
// ReSharper disable MemberCanBeMadeStatic.Local

using Microsoft.Extensions.Hosting;
using MQTTnet.Extensions.Hosting.Extensions;

namespace MQTTnet.Samples.Server;

public static class Server_Hosting_Extensions_Samples
{
    public static Task Start_Server()
    {
        var builder = new HostBuilder();

        builder.UseMqttServer(
            mqtt =>
            {
                mqtt.WithDefaultEndpoint();
            });

        var host = builder.Build();
        return host.RunAsync();
    }

    public static Task Start_Simple_Server()
    {
        var host = new HostBuilder().UseMqttServer().Build();

        return host.RunAsync();
    }

    // This could be called as a top-level statement in a Program.cs file
    public static Task Start_Single_Line_Server()
    {
        return new HostBuilder().UseMqttServer().Build().RunAsync();
    }
}