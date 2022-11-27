// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Formatter;

namespace MQTTnet.Tests.Formatter
{
    [TestClass]
    public sealed class MqttBufferReader_Tests
    {
        [TestMethod]
        public void Is_EndOfStream_Without_Buffer()
        {
            var reader = new MqttBufferReader();

            Assert.IsTrue(reader.EndOfStream);
            Assert.AreEqual(0, reader.BytesLeft);
        }

        [TestMethod]
        public void Report_Correct_Length_For_Full_Buffer()
        {
            var buffer = new byte[] { 5, 6, 7, 8, 9 };

            var reader = new MqttBufferReader();
            reader.SetBuffer(buffer, 0, 5);

            Assert.IsFalse(reader.EndOfStream);
            Assert.AreEqual(5, reader.BytesLeft);
        }

        [TestMethod]
        public void Report_Correct_Length_For_Partial_Buffer()
        {
            var buffer = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            var reader = new MqttBufferReader();

            // The used buffer contains more data than used!
            reader.SetBuffer(buffer, 5, 5);

            Assert.IsFalse(reader.EndOfStream);
            Assert.AreEqual(5, reader.BytesLeft);
        }
    }
}