// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using MQTTnet.Diagnostics;
using MQTTnet.Server;

namespace MQTTnet.Adapter
{
    public interface IMqttServerAdapter : IDisposable
    {
        Func<IMqttChannelAdapter, Task> ClientHandler { get; set; }

        Task StartAsync(MqttServerOptions options, IMqttNetLogger logger);

        Task StopAsync();
    }
}
