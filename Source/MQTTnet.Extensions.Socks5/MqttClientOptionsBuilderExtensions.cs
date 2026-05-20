// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet.Extensions.Socks5;

/// <summary>
///     Sugar extensions for configuring a SOCKS5 proxy on the MQTT client options builder.
/// </summary>
public static class MqttClientOptionsBuilderExtensions
{
    /// <summary>
    ///     Configures the client to tunnel its TCP connection to the broker through a SOCKS5
    ///     proxy. A TCP transport (via <c>WithTcpServer(...)</c> or <c>WithEndPoint(...)</c>)
    ///     must be configured first.
    /// </summary>
    public static MqttClientOptionsBuilder WithSocks5Proxy(this MqttClientOptionsBuilder builder, Socks5ProxyOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);

        return builder.WithStreamProvider(new Socks5StreamProvider(options));
    }

    /// <summary>
    ///     Configures the client to tunnel its TCP connection to the broker through a SOCKS5
    ///     proxy using the fluent <see cref="Socks5ProxyOptionsBuilder"/>.
    /// </summary>
    public static MqttClientOptionsBuilder WithSocks5Proxy(this MqttClientOptionsBuilder builder, Action<Socks5ProxyOptionsBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var optionsBuilder = new Socks5ProxyOptionsBuilder();
        configure(optionsBuilder);

        return WithSocks5Proxy(builder, optionsBuilder.Build());
    }
}
