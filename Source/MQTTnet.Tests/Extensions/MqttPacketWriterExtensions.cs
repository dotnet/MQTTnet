// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Formatter;
using MQTTnet.Protocol;

namespace MQTTnet.Tests.Extensions
{
    public static class MqttPacketWriterExtensions
    {
        public static byte[] AddMqttHeader(this MqttBufferWriter writer, MqttControlPacketType header, byte[] body)
        {
            writer.WriteByte(MqttBufferWriter.BuildFixedHeader(header));
            writer.WriteVariableByteInteger((uint)body.Length);
            writer.WriteBinary(body, 0, body.Length);
            return writer.GetBuffer();
        }
    }
}
