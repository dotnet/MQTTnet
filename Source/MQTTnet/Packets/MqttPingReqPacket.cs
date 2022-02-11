// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet.Packets
{
    public sealed class MqttPingReqPacket : MqttBasePacket
    {
        // This is a minor performance improvement.
        public static readonly MqttPingReqPacket Instance = new MqttPingReqPacket();

        public override string ToString()
        {
            return "PingReq";
        }
    }
}
