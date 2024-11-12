// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Server;
using System;

namespace Microsoft.AspNetCore.Builder;

public static class ApplicationBuilderExtensions
{    
    public static IApplicationBuilder UseMqttServer(this IApplicationBuilder app, Action<MqttServer> configure)
    {
        var server = app.ApplicationServices.GetRequiredService<MqttServer>();
        configure(server);
        return app;
    }
}