// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MQTTnet.Exceptions;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Client
{
    public sealed class MqttClientUnexpectedDisconnectReceivedException : MqttCommunicationException
    {
        public MqttClientUnexpectedDisconnectReceivedException(MqttDisconnectPacket disconnectPacket, Exception innerExcpetion = null) : base(
            $"Unexpected DISCONNECT (Reason code={disconnectPacket.ReasonCode}) received.",
            innerExcpetion)
        {
            ReasonCode = disconnectPacket.ReasonCode;
            SessionExpiryInterval = disconnectPacket.SessionExpiryInterval;
            ReasonString = disconnectPacket.ReasonString;
            ServerReference = disconnectPacket.ServerReference;
            UserProperties = disconnectPacket.UserProperties;
        }

        public MqttDisconnectReasonCode? ReasonCode { get; }

        public string ReasonString { get; }

        public string ServerReference { get; }

        public uint? SessionExpiryInterval { get; }

        public List<MqttUserProperty> UserProperties { get; }
    }
}