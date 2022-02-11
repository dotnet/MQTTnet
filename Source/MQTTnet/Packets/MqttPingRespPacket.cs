// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet.Packets
{
    public sealed class MqttPingRespPacket : MqttBasePacket
    {
        // This is a minor performance improvement.
        public static readonly MqttPingRespPacket Instance = new MqttPingRespPacket();

        public override string ToString()
        {
            return "PingResp";
        }
    }
}
