// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Sockets;

namespace MQTTnet.Client;

public sealed class MqttClientTcpOptions : IMqttClientChannelOptions
{
    public AddressFamily AddressFamily { get; set; } = AddressFamily.Unspecified;

    public int BufferSize { get; set; } = 8192;

    /// <summary>
    ///     Gets or sets whether the underlying socket should run in dual mode.
    ///     Leaving this _null_ will avoid setting this value at socket level.
    ///     Setting this a value other than _null_ will throw an exception when only IPv4 is supported on the machine.
    /// </summary>
    public bool? DualMode { get; set; }

    public LingerOption LingerState { get; set; } = new(true, 0);

    /// <summary>
    ///     Gets the local endpoint (network card) which is used by the client.
    ///     Set it to _null_ to let the OS select the network card.
    /// </summary>
    public EndPoint LocalEndpoint { get; set; }

    /// <summary>
    ///     Enables or disables the Nagle algorithm for the socket.
    ///     This is only supported for TCP.
    ///     For other protocol types the value is ignored.
    ///     Default: true
    /// </summary>
    public bool NoDelay { get; set; } = true;

    /// <summary>
    ///     The MQTT transport is usually TCP but when using other endpoint types like
    ///     unix sockets it must be changed (IP for unix sockets).
    /// </summary>
    public ProtocolType ProtocolType { get; set; } = ProtocolType.Tcp;

    public EndPoint RemoteEndpoint { get; set; }

    public MqttClientTlsOptions TlsOptions { get; set; } = new();

    public override string ToString()
    {
        return RemoteEndpoint?.ToString() ?? string.Empty;
    }
}