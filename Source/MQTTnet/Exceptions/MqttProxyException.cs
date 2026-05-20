// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet.Exceptions;

/// <summary>
///     Thrown when an <see cref="MQTTnet.Channel.IMqttClientStreamProvider"/> fails while
///     establishing the stream to the broker (proxy unreachable, authentication failure,
///     destination unreachable, protocol violation, ...).
///     Inherits from <see cref="MqttCommunicationException"/> so existing connect/reconnect
///     handling continues to work; callers that care about the specific reason can branch on
///     <see cref="ErrorCode"/>.
/// </summary>
public sealed class MqttProxyException : MqttCommunicationException
{
    public MqttProxyException(MqttProxyErrorCode errorCode, string message, string proxyAddress = null, Exception innerException = null)
        : base(BuildMessage(errorCode, message, proxyAddress), innerException)
    {
        ErrorCode = errorCode;
        ProxyAddress = proxyAddress;
    }

    /// <summary>Gets the categorized error reason.</summary>
    public MqttProxyErrorCode ErrorCode { get; }

    /// <summary>Gets the proxy address (host:port) associated with the failure, if known.</summary>
    public string ProxyAddress { get; }

    static string BuildMessage(MqttProxyErrorCode errorCode, string message, string proxyAddress)
    {
        if (string.IsNullOrEmpty(proxyAddress))
        {
            return $"Proxy error '{errorCode}': {message}";
        }

        return $"Proxy '{proxyAddress}' error '{errorCode}': {message}";
    }
}
