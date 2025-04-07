// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text;
using System;
using MQTTnet.Packets;

namespace MQTTnet.Server
{
    public sealed class MqttExtendedAuthenticationExchangeOptionsFactory
    {
        readonly MqttExtendedAuthenticationExchangeOptions _options = new MqttExtendedAuthenticationExchangeOptions();

        public MqttExtendedAuthenticationExchangeOptions Build()
        {
            return _options;
        }

        public MqttExtendedAuthenticationExchangeOptionsFactory WithAuthenticationData(byte[] authenticationData)
        {
            _options.AuthenticationData = authenticationData;

            return this;
        }

        public MqttExtendedAuthenticationExchangeOptionsFactory WithAuthenticationData(string authenticationData)
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

        public MqttExtendedAuthenticationExchangeOptionsFactory WithReasonString(string reasonString)
        {
            _options.ReasonString = reasonString;

            return this;
        }

        public MqttExtendedAuthenticationExchangeOptionsFactory WithUserProperties(List<MqttUserProperty> userProperties)
        {
            _options.UserProperties = userProperties;

            return this;
        }

        public MqttExtendedAuthenticationExchangeOptionsFactory WithUserProperty(string name, string value)
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
}
