// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MQTTnet.Packets;

namespace MQTTnet.Client
{
    public sealed class MqttClientDisconnectOptionsBuilder
    {
        MqttClientDisconnectOptionsReason _reason = MqttClientDisconnectOptionsReason.NormalDisconnection;
        string _reasonString;
        uint _sessionExpiryInterval;
        List<MqttUserProperty> _userProperties;

        public MqttClientDisconnectOptions Build()
        {
            return new MqttClientDisconnectOptions
            {
                Reason = _reason,
                ReasonString = _reasonString,
                UserProperties = _userProperties,
                SessionExpiryInterval = _sessionExpiryInterval
            };
        }

        public MqttClientDisconnectOptionsBuilder WithReason(MqttClientDisconnectOptionsReason value)
        {
            _reason = value;
            return this;
        }

        public MqttClientDisconnectOptionsBuilder WithReasonString(string value)
        {
            _reasonString = value;
            return this;
        }

        public MqttClientDisconnectOptionsBuilder WithSessionExpiryInterval(uint value)
        {
            _sessionExpiryInterval = value;
            return this;
        }

        public MqttClientDisconnectOptionsBuilder WithUserProperties(List<MqttUserProperty> userProperties)
        {
            _userProperties = userProperties;
            return this;
        }

        public MqttClientDisconnectOptionsBuilder WithUserProperty(string name, string value)
        {
            if (_userProperties == null)
            {
                _userProperties = new List<MqttUserProperty>();
            }

            _userProperties.Add(new MqttUserProperty(name, value));
            return this;
        }
    }
}