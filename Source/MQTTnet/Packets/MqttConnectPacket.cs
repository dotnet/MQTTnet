// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MQTTnet.Protocol;

namespace MQTTnet.Packets
{
    public sealed class MqttConnectPacket : MqttBasePacket
    {
        public byte[] AuthenticationData { get; set; }

        public string AuthenticationMethod { get; set; }

        /// <summary>
        ///     Also called "Clean Start" in MQTTv5.
        /// </summary>
        public bool CleanSession { get; set; }

        public string ClientId { get; set; }

        public byte[] WillCorrelationData { get; set; }

        public ushort KeepAlivePeriod { get; set; }
        
        public uint MaximumPacketSize { get; set; }

        public byte[] Password { get; set; }

        public ushort ReceiveMaximum { get; set; }

        public bool RequestProblemInformation { get; set; }

        public bool RequestResponseInformation { get; set; }

        public string WillResponseTopic { get; set; }

        public uint SessionExpiryInterval { get; set; }

        public ushort TopicAliasMaximum { get; set; }

        public string Username { get; set; }

        public List<MqttUserProperty> UserProperties { get; set; }

        public string WillContentType { get; set; }

        public uint WillDelayInterval { get; set; }

        public bool WillFlag { get; set; }

        public byte[] WillMessage { get; set; }

        public uint WillMessageExpiryInterval { get; set; }

        public MqttPayloadFormatIndicator WillPayloadFormatIndicator { get; set; } = MqttPayloadFormatIndicator.Unspecified;

        public MqttQualityOfServiceLevel WillQoS { get; set; } = MqttQualityOfServiceLevel.AtMostOnce;

        public bool WillRetain { get; set; }

        public string WillTopic { get; set; }

        public List<MqttUserProperty> WillUserProperties { get; set; }

        public override string ToString()
        {
            var passwordText = string.Empty;

            if (Password != null)
            {
                passwordText = "****";
            }

            return $"Connect: [ClientId={ClientId}] [Username={Username}] [Password={passwordText}] [KeepAlivePeriod={KeepAlivePeriod}] [CleanSession={CleanSession}]";
        }
    }
}