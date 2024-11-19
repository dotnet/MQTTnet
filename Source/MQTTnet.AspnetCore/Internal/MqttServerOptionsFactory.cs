// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using MQTTnet.Server;
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace MQTTnet.AspNetCore
{
    sealed class MqttServerOptionsFactory : MqttOptionsFactory<MqttServerOptionsBuilder, MqttServerOptions>
    {
        public MqttServerOptionsFactory(
            IOptions<MqttServerOptionsBuilder> optionsBuilderOptions,
            IEnumerable<IConfigureOptions<MqttServerOptions>> setups,
            IEnumerable<IPostConfigureOptions<MqttServerOptions>> postConfigures)
            : base(optionsBuilderOptions, setups, postConfigures)
        {
        }

        protected override MqttServerOptions CreateOptions()
        {
            return base.OptionsBuilder.Build();
        }
    }
}
