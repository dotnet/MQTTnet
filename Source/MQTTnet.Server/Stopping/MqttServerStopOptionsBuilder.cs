// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace MQTTnet.Server
{
    public sealed class MqttServerStopOptionsBuilder
    {
        readonly MqttServerStopOptions _options = new MqttServerStopOptions();

        public MqttServerStopOptionsBuilder WithDefaultClientDisconnectOptions(MqttServerClientDisconnectOptions value)
        {
            _options.DefaultClientDisconnectOptions = value;
            return this;
        }
        
        public MqttServerStopOptionsBuilder WithDefaultClientDisconnectOptions(Action<MqttServerClientDisconnectOptionsBuilder> builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var optionsBuilder = new MqttServerClientDisconnectOptionsBuilder();
            builder.Invoke(optionsBuilder);
            
            _options.DefaultClientDisconnectOptions = optionsBuilder.Build();
            return this;
        }
        
        public MqttServerStopOptions Build()
        {
            return _options;
        }
    }
}