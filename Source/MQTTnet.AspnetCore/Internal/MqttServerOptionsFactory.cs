// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using MQTTnet.Server;
using System.Collections.Generic;

namespace MQTTnet.AspNetCore
{
    sealed class MqttServerOptionsFactory : MqttOptionsFactory<MqttServerOptions>
    {
        public MqttServerOptionsFactory(
            IOptions<MqttServerOptionsBuilder> optionsBuilderOptions,
            IEnumerable<IConfigureOptions<MqttServerOptions>> setups,
            IEnumerable<IPostConfigureOptions<MqttServerOptions>> postConfigures)
            : base(optionsBuilderOptions.Value.Build, setups, postConfigures)
        {
        }
    }
}
