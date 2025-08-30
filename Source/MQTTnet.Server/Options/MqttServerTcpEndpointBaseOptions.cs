// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Sockets;

namespace MQTTnet.Server;

public abstract class MqttServerTcpEndpointBaseOptions
{
    public bool IsEnabled { get; set; }

    public int Port { get; set; }

    public int ConnectionBacklog { get; set; } = 100;

    public bool NoDelay { get; set; } = true;

    /// <summary>
    ///     Gets or sets whether the sockets keep alive feature should be used.
    ///     The value _null_ indicates that the OS and framework defaults should be used.
    /// </summary>
    public bool? KeepAlive { get; set; }

    /// <summary>
    ///     Usually the MQTT packets can be send partially. This is done by using multiple TCP packets
    ///     or WebSocket frames etc. Unfortunately not all clients do support this feature and
    ///     will close the connection when receiving such packets. If such clients are connecting to this
    ///     server the flag must be set to _false_.
    /// </summary>
    public bool AllowPacketFragmentation { get; set; } = true;

    /// <summary>
    ///     Gets or sets the TCP keep alive interval.
    ///     The value _null_ indicates that the OS and framework defaults should be used.
    /// </summary>
    public int? TcpKeepAliveInterval { get; set; }

    /// <summary>
    ///     Gets or sets the TCP keep alive retry count.
    ///     The value _null_ indicates that the OS and framework defaults should be used.
    /// </summary>
    public int? TcpKeepAliveRetryCount { get; set; }

    /// <summary>
    ///     Gets or sets the TCP keep alive time.
    ///     The value _null_ indicates that the OS and framework defaults should be used.
    /// </summary>
    public int? TcpKeepAliveTime { get; set; }

    public LingerOption LingerState { get; set; } = new LingerOption(true, 0);

    public IPAddress BoundInterNetworkAddress { get; set; } = IPAddress.Any;

    public IPAddress BoundInterNetworkV6Address { get; set; } = IPAddress.IPv6Any;

    /// <summary>
    ///     This requires admin permissions on Linux.
    /// </summary>
    public bool ReuseAddress { get; set; }
}