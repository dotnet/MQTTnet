// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Formatter;
using System;

namespace MQTTnet.Tests
{
    [TestClass]
    public sealed class Protocol_Tests
    {
        [TestMethod]
        public void Encode_Four_Byte_Integer()
        {
            var writer = new MqttBufferWriter(4, 4);

            for (uint value = 0; value < 268435455; value++)
            {
                writer.WriteVariableByteInteger(value);

                var buffer = writer.GetWrittenMemory();

                var reader = new MqttBufferReader();
                reader.SetBuffer(buffer);
                var checkValue = reader.ReadVariableByteInteger();

                Assert.AreEqual(value, checkValue);

                writer.Reset(0);
            }
        }

        [TestMethod]
        public void Encode_Two_Byte_Integer()
        {
            var writer = new MqttBufferWriter(2, 2);

            for (ushort value = 0; value < ushort.MaxValue; value++)
            {
                writer.WriteTwoByteInteger(value);

                var buffer = writer.GetWrittenMemory();

                var reader = new MqttBufferReader();
                reader.SetBuffer(buffer);
                var checkValue = reader.ReadTwoByteInteger();

                Assert.AreEqual(value, checkValue);

                writer.Reset(0);
            }
        }
    }
}