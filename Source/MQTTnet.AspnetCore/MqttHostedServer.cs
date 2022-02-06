// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Server;

namespace MQTTnet.AspNetCore
{
    public sealed class MqttHostedServer : MqttServer, IHostedService
    {
        public MqttHostedServer(MqttServerOptions options, IEnumerable<IMqttServerAdapter> adapters, IMqttNetLogger logger) 
            : base(options, adapters, logger)
        {
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _ =  StartAsync();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return StopAsync();
        }
    }
}