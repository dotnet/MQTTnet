// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet;

/// <summary>
/// Mqtt 客户端连接状态
/// </summary>
public enum MqttClientConnectionStatus
{
    /// <summary>
    /// 已断开
    /// </summary>
    Disconnected,

    /// <summary>
    /// 断开进行中
    /// </summary>
    Disconnecting,

    /// <summary>
    /// 已连接
    /// </summary>
    Connected,

    /// <summary>
    /// 连接进行中
    /// </summary>
    Connecting
}