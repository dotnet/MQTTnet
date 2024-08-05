// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet;

public class MqttExtendedAuthenticationExchangeContext
{
    public MqttExtendedAuthenticationExchangeContext(MqttAuthPacket authPacket, MqttClient client)
    {
        if (authPacket == null)
        {
            throw new ArgumentNullException(nameof(authPacket));
        }

        ReasonCode = authPacket.ReasonCode;
        ReasonString = authPacket.ReasonString;
        AuthenticationMethod = authPacket.AuthenticationMethod;
        AuthenticationData = authPacket.AuthenticationData;
        UserProperties = authPacket.UserProperties;

        Client = client ?? throw new ArgumentNullException(nameof(client));
    }

    /// <summary>
    ///     Gets the authentication data.
    ///     Hint: MQTT 5 feature only.
    /// </summary>
    public byte[] AuthenticationData { get; }

    /// <summary>
    ///     Gets the authentication method.
    ///     Hint: MQTT 5 feature only.
    /// </summary>
    public string AuthenticationMethod { get; }

    public MqttClient Client { get; }

    /// <summary>
    ///     Gets the reason code.
    ///     Hint: MQTT 5 feature only.
    /// </summary>
    public MqttAuthenticateReasonCode ReasonCode { get; }

    /// <summary>
    ///     Gets the reason string.
    ///     Hint: MQTT 5 feature only.
    /// </summary>
    public string ReasonString { get; }

    /// <summary>
    ///     Gets the user properties.
    ///     In MQTT 5, user properties are basic UTF-8 string key-value pairs that you can append to almost every type of MQTT
    ///     packet.
    ///     As long as you don’t exceed the maximum message size, you can use an unlimited number of user properties to add
    ///     metadata to MQTT messages and pass information between publisher, broker, and subscriber.
    ///     The feature is very similar to the HTTP header concept.
    ///     Hint: MQTT 5 feature only.
    /// </summary>
    public List<MqttUserProperty> UserProperties { get; }
}