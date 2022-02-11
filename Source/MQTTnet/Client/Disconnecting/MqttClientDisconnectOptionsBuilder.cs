// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet.Client
{
    public sealed class MqttClientDisconnectOptionsBuilder
    {
        MqttClientDisconnectReason _reason = MqttClientDisconnectReason.NormalDisconnection;
        string _reasonString;
        
        public MqttClientDisconnectOptionsBuilder WithReasonString(string value)
        {
            _reasonString = value;
            return this;
        }
        
        public MqttClientDisconnectOptionsBuilder WithReason(MqttClientDisconnectReason value)
        {
            _reason = value;
            return this;
        }
        
        public MqttClientDisconnectOptions Build()
        {
            return new MqttClientDisconnectOptions
            {
                Reason = _reason,
                ReasonString = _reasonString
            };
        }
    }
}