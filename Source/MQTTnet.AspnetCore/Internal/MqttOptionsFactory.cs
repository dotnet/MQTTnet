// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

namespace MQTTnet.AspNetCore
{
    class MqttOptionsFactory<TOptions> where TOptions : class
    {
        private readonly Func<TOptions> _defaultOptionsFactory;
        private readonly IEnumerable<IConfigureOptions<TOptions>> _setups;
        private readonly IEnumerable<IPostConfigureOptions<TOptions>> _postConfigures;

        public MqttOptionsFactory(
            Func<TOptions> defaultOptionsFactory,
            IEnumerable<IConfigureOptions<TOptions>> setups,
            IEnumerable<IPostConfigureOptions<TOptions>> postConfigures)
        {
            _defaultOptionsFactory = defaultOptionsFactory;
            _setups = setups;
            _postConfigures = postConfigures;
        }

        public TOptions CreateOptions()
        {
            var options = _defaultOptionsFactory();
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
    }
}
