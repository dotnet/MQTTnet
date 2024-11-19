// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using System.Collections.Generic;

namespace MQTTnet.AspNetCore
{
    abstract class MqttOptionsFactory<TOptionsBuilder, TOptions>
        where TOptionsBuilder : class
        where TOptions : class
    {
        private readonly IEnumerable<IConfigureOptions<TOptions>> _setups;
        private readonly IEnumerable<IPostConfigureOptions<TOptions>> _postConfigures;
        protected TOptionsBuilder OptionsBuilder { get; }

        public MqttOptionsFactory(
            IOptions<TOptionsBuilder> optionsBuilderOptions,
            IEnumerable<IConfigureOptions<TOptions>> setups,
            IEnumerable<IPostConfigureOptions<TOptions>> postConfigures)
        {
            OptionsBuilder = optionsBuilderOptions.Value;
            _setups = setups;
            _postConfigures = postConfigures;
        }

        public TOptions Build()
        {
            var options = CreateOptions();
            var name = Options.DefaultName;

            foreach (var setup in _setups)
            {
                if (setup is IConfigureNamedOptions<TOptions> namedSetup)
                {
                    namedSetup.Configure(name, options);
                }
                else if (name == Options.DefaultName)
                {
                    setup.Configure(options);
                }
            }
            foreach (var post in _postConfigures)
            {
                post.PostConfigure(name, options);
            }
            return options;
        }

        protected abstract TOptions CreateOptions();
    }
}
