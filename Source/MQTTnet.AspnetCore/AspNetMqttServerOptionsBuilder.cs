// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using MQTTnet.Server;

namespace MQTTnet.AspNetCore;

public sealed class AspNetMqttServerOptionsBuilder : MqttServerOptionsBuilder
{
    public AspNetMqttServerOptionsBuilder(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public IServiceProvider ServiceProvider { get; }
}