// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet.Formatter
{
    public interface IMqttPacketWriter
    {
        int Length { get; }

        void WriteWithLengthPrefix(string value);

        void Write(byte value);

        void WriteWithLengthPrefix(byte[] value);

        void Write(ushort value);

        void Write(IMqttPacketWriter value);

        void WriteVariableLengthInteger(uint value);

        void Write(byte[] value, int offset, int length);

        void Reset(int length);

        void Seek(int offset);

        void FreeBuffer();

        byte[] GetBuffer();
    }
}
