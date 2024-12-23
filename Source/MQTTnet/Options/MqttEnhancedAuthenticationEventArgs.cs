// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Adapter;
using MQTTnet.Exceptions;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet;

public class MqttEnhancedAuthenticationEventArgs : EventArgs
{
    readonly IMqttChannelAdapter _channelAdapter;
    readonly MqttAuthPacket _initialAuthPacket;

    public MqttEnhancedAuthenticationEventArgs(MqttAuthPacket initialAuthPacket, IMqttChannelAdapter channelAdapter, CancellationToken cancellationToken)
    {
        _initialAuthPacket = initialAuthPacket ?? throw new ArgumentNullException(nameof(initialAuthPacket));
        _channelAdapter = channelAdapter ?? throw new ArgumentNullException(nameof(channelAdapter));

        CancellationToken = cancellationToken;
    }

    /// <summary>
    ///     Gets the authentication data.
    ///     Hint: MQTT 5 feature only.
    /// </summary>
    public byte[] AuthenticationData => _initialAuthPacket.AuthenticationData;

    /// <summary>
    ///     Gets the authentication method.
    ///     Hint: MQTT 5 feature only.
    /// </summary>
    public string AuthenticationMethod => _initialAuthPacket.AuthenticationMethod;

    public CancellationToken CancellationToken { get; }

    /// <summary>
    ///     Gets the reason code.
    ///     Hint: MQTT 5 feature only.
    /// </summary>
    public MqttAuthenticateReasonCode ReasonCode => _initialAuthPacket.ReasonCode;

    /// <summary>
    ///     Gets the reason string.
    ///     Hint: MQTT 5 feature only.
    /// </summary>
    public string ReasonString => _initialAuthPacket.ReasonString;

    /// <summary>
    ///     Gets the user properties.
    ///     In MQTT 5, user properties are basic UTF-8 string key-value pairs that you can append to almost every type of MQTT
    ///     packet.
    ///     As long as you don’t exceed the maximum message size, you can use an unlimited number of user properties to add
    ///     metadata to MQTT messages and pass information between publisher, broker, and subscriber.
    ///     The feature is very similar to the HTTP header concept.
    ///     Hint: MQTT 5 feature only.
    /// </summary>
    public List<MqttUserProperty> UserProperties => _initialAuthPacket.UserProperties;

    public async Task<ReceiveMqttEnhancedAuthenticationDataResult> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        var receivedPacket = await _channelAdapter.ReceivePacketAsync(cancellationToken).ConfigureAwait(false);

        if (receivedPacket is MqttAuthPacket authPacket)
        {
            return new ReceiveMqttEnhancedAuthenticationDataResult
            {
                AuthenticationData = authPacket.AuthenticationData,
                AuthenticationMethod = authPacket.AuthenticationMethod,
                ReasonString = authPacket.ReasonString,
                ReasonCode = authPacket.ReasonCode,
                UserProperties = authPacket.UserProperties
            };
        }

        if (receivedPacket is MqttConnAckPacket)
        {
            throw new InvalidOperationException("The enhanced authentication handler must not wait for the CONNACK packet.");
        }

        throw new MqttProtocolViolationException("Received other packet than AUTH while authenticating.");
    }

    public Task SendAsync(SendMqttEnhancedAuthenticationDataOptions options, CancellationToken cancellationToken = default)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var authPacket = new MqttAuthPacket
        {
            ReasonCode = MqttAuthenticateReasonCode.ContinueAuthentication,
            AuthenticationMethod = AuthenticationMethod,
            AuthenticationData = options.Data,
            UserProperties = options.UserProperties,
            ReasonString = options.ReasonString
        };

        return _channelAdapter.SendPacketAsync(authPacket, cancellationToken);
    }
}