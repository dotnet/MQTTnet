// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Protocol;

namespace MQTTnet.Packets
{
    public sealed class MqttConnectPacket : MqttBasePacket
    {
        public string ClientId { get; set; }

        public string Username { get; set; }

        public byte[] Password { get; set; }

        public ushort KeepAlivePeriod { get; set; }

       /// <summary>
       /// Also called "Clean Start" in MQTTv5.
       /// </summary>
        public bool CleanSession { get; set; }
        
        /// <summary>
        /// Added in MQTTv5.
        /// </summary>
        public MqttConnectPacketProperties Properties { get; } = new MqttConnectPacketProperties();

        public bool WillFlag { get; set; }
        
        public string WillTopic { get; set; }
        
        public byte[] WillMessage { get; set; }
        
        public MqttQualityOfServiceLevel WillQoS { get; set; } = MqttQualityOfServiceLevel.AtMostOnce;
        
        public bool WillRetain { get; set; }

        public MqttConnectPacketWillProperties WillProperties { get; } = new MqttConnectPacketWillProperties();
        
        public override string ToString()
        {
            var passwordText = string.Empty;

            if (Password != null)
            {
                passwordText = "****";
            }

            return string.Concat("Connect: [ClientId=", ClientId, "] [Username=", Username, "] [Password=", passwordText, "] [KeepAlivePeriod=", KeepAlivePeriod, "] [CleanSession=", CleanSession, "]");
        }
    }
}
