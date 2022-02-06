// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet.Formatter
{
    public interface IMqttPacketBodyReader
    {
        int Length { get; }

        int Offset { get; }

        bool EndOfStream { get; }

        byte ReadByte();

        byte[] ReadRemainingData();

        ushort ReadTwoByteInteger();

        string ReadStringWithLengthPrefix();

        byte[] ReadWithLengthPrefix();

        uint ReadFourByteInteger();

        uint ReadVariableLengthInteger();

        bool ReadBoolean();

        void Seek(int position);
    }
}
