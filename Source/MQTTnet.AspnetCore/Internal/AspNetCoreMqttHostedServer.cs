// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore
{
    sealed class AspNetCoreMqttHostedServer : IHostedService
    {
        private readonly AspNetCoreMqttServer _aspNetCoreMqttServer;

        public AspNetCoreMqttHostedServer(
            AspNetCoreMqttServer aspNetCoreMqttServer,
            IHostApplicationLifetime hostApplicationLifetime)
        {
            _aspNetCoreMqttServer = aspNetCoreMqttServer;
            hostApplicationLifetime.ApplicationStarted.Register(ApplicationStarted);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private void ApplicationStarted()
        {
            _ = _aspNetCoreMqttServer.StartAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return _aspNetCoreMqttServer.StopAsync();
        }
    }
}
