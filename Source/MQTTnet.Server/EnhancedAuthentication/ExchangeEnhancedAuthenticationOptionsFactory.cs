// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using MQTTnet.Packets;

namespace MQTTnet.Server.EnhancedAuthentication;

public sealed class ExchangeEnhancedAuthenticationOptionsFactory
{
    readonly ExchangeEnhancedAuthenticationOptions _options = new();

    public ExchangeEnhancedAuthenticationOptions Build()
    {
        return _options;
    }

    public ExchangeEnhancedAuthenticationOptionsFactory WithAuthenticationData(byte[] authenticationData)
    {
        _options.AuthenticationData = authenticationData;

        return this;
    }

    public ExchangeEnhancedAuthenticationOptionsFactory WithAuthenticationData(string authenticationData)
    {
        if (authenticationData == null)
        {
            _options.AuthenticationData = null;
        }
        else
        {
            _options.AuthenticationData = Encoding.UTF8.GetBytes(authenticationData);
        }

        return this;
    }

    public ExchangeEnhancedAuthenticationOptionsFactory WithReasonString(string reasonString)
    {
        _options.ReasonString = reasonString;

        return this;
    }

    public ExchangeEnhancedAuthenticationOptionsFactory WithUserProperties(List<MqttUserProperty> userProperties)
    {
        _options.UserProperties = userProperties;

        return this;
    }

    public ExchangeEnhancedAuthenticationOptionsFactory WithUserProperty(string name, string value)
    {
        if (name == null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        if (_options.UserProperties == null)
        {
            _options.UserProperties = new List<MqttUserProperty>();
        }

        _options.UserProperties.Add(new MqttUserProperty(name, value));

        return this;
    }
}