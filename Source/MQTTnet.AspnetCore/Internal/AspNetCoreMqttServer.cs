// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Server;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore;

sealed class AspNetCoreMqttServer : MqttServer
{
    private readonly IOptions<MqttServerStopOptionsBuilder> _stopOptions;

    public AspNetCoreMqttServer(
        IOptions<MqttServerOptionsBuilder> serverOptions,
        IOptions<MqttServerStopOptionsBuilder> stopOptions,
        IEnumerable<IMqttServerAdapter> adapters,
        IMqttNetLogger logger) : base(serverOptions.Value.Build(), adapters, logger)
    {
        _stopOptions = stopOptions;
    }

    public Task StopAsync()
    {
        return base.StopAsync(_stopOptions.Value.Build());
    }
}