// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet.Server;

public sealed class MqttServerStopOptionsBuilder
{
    readonly MqttServerStopOptions _options = new();

    public MqttServerStopOptionsBuilder WithDefaultClientDisconnectOptions(MqttServerClientDisconnectOptions value)
    {
        _options.DefaultClientDisconnectOptions = value;
        return this;
    }

    public MqttServerStopOptionsBuilder WithDefaultClientDisconnectOptions(Action<MqttServerClientDisconnectOptionsBuilder> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var optionsBuilder = new MqttServerClientDisconnectOptionsBuilder();
        builder(optionsBuilder);

        _options.DefaultClientDisconnectOptions = optionsBuilder.Build();
        return this;
    }

    public MqttServerStopOptions Build()
    {
        return _options;
    }
}