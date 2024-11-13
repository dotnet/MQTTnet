// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Server;
using System;

namespace MQTTnet.AspNetCore;

public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Get and use MqttServer
    /// Also, you can inject MqttServer into your service
    /// </summary>
    /// <param name="app"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseMqttServer(this IApplicationBuilder app, Action<MqttServer> configure)
    {
        var server = app.ApplicationServices.GetRequiredService<MqttServer>();
        configure(server);
        return app;
    }
}