// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MQTTnet.Exceptions;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Client
{
    public sealed class MqttClientUnexpectedDisconnectReceivedException : MqttCommunicationException
    {
        public MqttClientUnexpectedDisconnectReceivedException(MqttDisconnectPacket disconnectPacket) 
            : base($"Unexpected DISCONNECT (Reason code={disconnectPacket.ReasonCode}) received.")
        {
            ReasonCode = disconnectPacket.ReasonCode;
            SessionExpiryInterval = disconnectPacket.SessionExpiryInterval;
            ReasonString = disconnectPacket.ReasonString;
            ServerReference = disconnectPacket.ServerReference;
            UserProperties = disconnectPacket.UserProperties;
        }

        public MqttDisconnectReasonCode? ReasonCode { get; }

        public uint? SessionExpiryInterval { get; }

        public string ReasonString { get; }

        public List<MqttUserProperty> UserProperties { get; }

        public string ServerReference { get; }
    }
}
