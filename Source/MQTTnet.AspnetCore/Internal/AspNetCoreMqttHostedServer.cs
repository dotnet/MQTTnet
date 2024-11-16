// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore
{
    sealed class AspNetCoreMqttHostedServer : BackgroundService
    {
        private readonly AspNetCoreMqttServer _aspNetCoreMqttServer;
        private readonly Task _applicationStartedTask;

        public AspNetCoreMqttHostedServer(
            AspNetCoreMqttServer aspNetCoreMqttServer,
            IHostApplicationLifetime hostApplicationLifetime)
        {
            _aspNetCoreMqttServer = aspNetCoreMqttServer;
            _applicationStartedTask = WaitApplicationStartedAsync(hostApplicationLifetime);
        }

        private static Task WaitApplicationStartedAsync(IHostApplicationLifetime hostApplicationLifetime)
        {
            var taskCompletionSource = new TaskCompletionSource();
            hostApplicationLifetime.ApplicationStarted.Register(OnApplicationStarted);
            return taskCompletionSource.Task;

            void OnApplicationStarted()
            {
                taskCompletionSource.TrySetResult();
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _applicationStartedTask.WaitAsync(stoppingToken);
            await _aspNetCoreMqttServer.StartAsync(stoppingToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            return _aspNetCoreMqttServer.StopAsync(cancellationToken);
        }
    }
}
