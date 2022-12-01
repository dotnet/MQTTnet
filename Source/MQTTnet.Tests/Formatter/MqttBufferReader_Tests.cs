// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using System;
using System.Collections.Generic;

namespace MQTTnet.Tests.Formatter
{
    [TestClass]
    public sealed class MqttBufferReader_Tests
    {
        // Helper class to build up a reference to elements of various types in a buffer
        class ElementReference
        {
            public const int NumBufferElementTypes = 5;
            public enum BufferElementType { Byte, TwoByteInt, FourByteInt, VariableSizeInt, String };

            public ElementReference(BufferElementType type, int size, uint numberValue, string stringValue, int bufferOffset)
            {
                Type = type;
                Size = size;
                NumberValue = numberValue;
                StringValue = stringValue;
                BufferOffset = bufferOffset;
            }
            public BufferElementType Type { get; }
            public int Size { get; }
            public uint NumberValue { get; }
            public string StringValue { get; }
            public int BufferOffset { get; }
        }


        [TestMethod]
        [ExpectedException(typeof(MqttProtocolViolationException), "Expected at least 4 bytes but there are only 3 bytes")]
        public void Fire_Exception_If_Not_Enough_Data()
        {
            var buffer = new byte[] { 0, 1, 2 };

            var reader = new MqttBufferReader();

            reader.SetBuffer(buffer, 0, 3);

            // 1 byte is missing.
            reader.ReadFourByteInteger();
        }

        [TestMethod]
        [ExpectedException(typeof(MqttProtocolViolationException), "Expected at least 4 bytes but there are only 3 bytes")]
        public void Fire_Exception_If_Not_Enough_Data_With_Longer_Buffer()
        {
            var buffer = new byte[] { 0, 1, 2, 3, 4, 5, 6 };

            var reader = new MqttBufferReader();

            reader.SetBuffer(buffer, 0, 3);

            // 1 byte is missing.
            reader.ReadFourByteInteger();
        }

        [TestMethod]
        public void Is_EndOfStream_Without_Buffer()
        {
            var reader = new MqttBufferReader();

            Assert.IsTrue(reader.EndOfStream);
            Assert.AreEqual(0, reader.BytesLeft);
        }

        [TestMethod]
        public void Read_Remaining_Data_From_Larger_Buffer()
        {
            var buffer = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            var reader = new MqttBufferReader();

            // The used buffer contains more data than used!
            reader.SetBuffer(buffer, 0, 5);

            // This should only read 5 bytes even if more data is in the buffer
            // due to custom bounds.
            var remainingData = reader.ReadRemainingData();

            Assert.IsTrue(reader.EndOfStream);
            Assert.AreEqual(0, reader.BytesLeft);
            Assert.AreEqual(5, remainingData.Length);
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
        public void Report_Correct_Length_For_Partial_End_Buffer()
        {
            var buffer = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            var reader = new MqttBufferReader();

            // The used buffer contains more data than used!
            reader.SetBuffer(buffer, 5, 5);

            Assert.IsFalse(reader.EndOfStream);
            Assert.AreEqual(5, reader.BytesLeft);
        }

        [TestMethod]
        public void Report_Correct_Length_For_Partial_Start_Buffer()
        {
            var buffer = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            var reader = new MqttBufferReader();

            // The used buffer contains more data than used!
            reader.SetBuffer(buffer, 0, 5);

            Assert.IsFalse(reader.EndOfStream);
            Assert.AreEqual(5, reader.BytesLeft);
        }

        /// <summary>
        /// Test reading random sequences of types and values from the MqttPacketReader
        /// </summary>
        [TestMethod]
        public void Read_Various_Positions_and_Offsets()
        {
            const int NumBufferElements = 1000;
            const int NumTestSequences = 10000;

            const string TestString = "The quick brown fox jumps over the lazy dog.";

            var rndSeed = Environment.TickCount;
            // rndSeed = ...; // assign random seed here if there is an error so that it can be repeated
            var rnd = new Random(rndSeed);
            // rndSeed is printed when an assert fails

            var bufferElements = new List<ElementReference>();

            byte[] elementBuffer;

            // Fill a buffer with strings or numbers in big endian format
            using (var ms = new System.IO.MemoryStream())
            {
                using (var bw = new System.IO.BinaryWriter(ms))
                {
                    var bufferOffset = 0;

                    for (var i = 0; i < NumBufferElements; i++)
                    {
                        if (i > 654)
                        {
                            int x = 0;
                        }
                        var nextElementType = (ElementReference.BufferElementType)rnd.Next(ElementReference.NumBufferElementTypes);

                        byte[] elementBytes = null;
                        uint elementNumberValue = 0; // all element number values fit into uint
                        string elementStringValue = null;
                        int elementSize = 0;

                        var alreadyBigEndian = false;
                        switch (nextElementType)
                        {
                            case ElementReference.BufferElementType.Byte:
                                var byteValue = (byte)i;
                                elementNumberValue = byteValue;
                                elementSize = sizeof(byte);
                                elementBytes = new byte[1] { byteValue };
                                alreadyBigEndian = true; // nothing to swap
                                break;
                            case ElementReference.BufferElementType.TwoByteInt:
                                var ushortValue = (ushort)i;
                                elementNumberValue = ushortValue;
                                elementSize = sizeof(ushort);
                                elementBytes = BitConverter.GetBytes(ushortValue);
                                break;
                            case ElementReference.BufferElementType.FourByteInt:
                                var uintValue = (uint)i;
                                elementNumberValue = uintValue;
                                elementSize = sizeof(uint);
                                elementBytes = BitConverter.GetBytes(uintValue);
                                break;
                            case ElementReference.BufferElementType.VariableSizeInt:
                                {
                                    elementNumberValue = (uint)i;
                                    var writer = new MqttBufferWriter(4, 4);
                                    writer.WriteVariableByteInteger(elementNumberValue);
                                    elementSize = writer.Length;
                                    elementBytes = new byte[elementSize];
                                    var buffer = writer.GetBuffer();
                                    Array.Copy(buffer, elementBytes, elementSize);
                                    alreadyBigEndian = true; // nothing to swap
                                }
                                break;
                            case ElementReference.BufferElementType.String:
                                {
                                    var stringLen = rnd.Next(TestString.Length);
                                    elementStringValue = TestString.Substring(0, stringLen); // could be empty
                                    var writer = new MqttBufferWriter(stringLen + 1, stringLen + 1);
                                    writer.WriteString(elementStringValue);
                                    elementSize = writer.Length;
                                    elementBytes = new byte[elementSize];
                                    var buffer = writer.GetBuffer();
                                    Array.Copy(buffer, elementBytes, elementSize);
                                    alreadyBigEndian = true; // nothing to swap
                                }
                                break;

                        }

                        if (BitConverter.IsLittleEndian && (!alreadyBigEndian))
                        {
                            Array.Reverse(elementBytes);
                        }

                        bw.Write(elementBytes);
                        bufferElements.Add(new ElementReference(nextElementType, elementSize, elementNumberValue, elementStringValue, bufferOffset));
                        bufferOffset += elementSize;
                    }
                    elementBuffer = ms.ToArray();
                }
            }

            for (var i = 0; i < NumTestSequences; i++)
            {
                var firstElementIndex = rnd.Next(NumBufferElements - 1);
                // segmentElementIndex will be < NumBufferElements - 1, which means there is at least one byte left for segment element count
                var elementCount = rnd.Next(NumBufferElements - firstElementIndex) + 1;
                var lastElementIndex = firstElementIndex + elementCount - 1;
                // Use element index to get buffer offsets
                var lastElement = bufferElements[lastElementIndex];
                var segmentStartPosition = bufferElements[firstElementIndex].BufferOffset;
                var segmentEndPosition = lastElement.BufferOffset + lastElement.Size;
                var segmentLength = segmentEndPosition - segmentStartPosition;

                var reader = new MqttBufferReader();
                reader.SetBuffer(elementBuffer, segmentStartPosition, segmentLength);

                // read all elements in the buffer segment; values should be as expected              
                for (var n = 0; n < elementCount; n++)
                {
                    var element = bufferElements[firstElementIndex + n];
                    uint elementNumberValue = 0;
                    string elementStringValue = null;

                    var expectedPosition = element.BufferOffset - segmentStartPosition;
                    Assert.AreEqual(expectedPosition, reader.Position);

                    switch (element.Type)
                    {
                        case ElementReference.BufferElementType.Byte:
                            {
                                elementNumberValue = reader.ReadByte();
                            }
                            break;
                        case ElementReference.BufferElementType.TwoByteInt:
                            {
                                elementNumberValue = reader.ReadTwoByteInteger();
                            }
                            break;
                        case ElementReference.BufferElementType.FourByteInt:
                            {
                                elementNumberValue = reader.ReadFourByteInteger();
                            }
                            break;
                        case ElementReference.BufferElementType.VariableSizeInt:
                            {
                                elementNumberValue = reader.ReadVariableByteInteger();
                            }
                            break;
                        case ElementReference.BufferElementType.String:
                            {
                                elementStringValue = reader.ReadString();
                            }
                            break;
                    }

                    if (elementStringValue != null)
                    {
                        Assert.AreEqual(elementStringValue, element.StringValue, $"'{elementStringValue}' not equal '{element.StringValue}' with random seed {rndSeed}.");
                    }
                    else
                    {
                        Assert.AreEqual(elementNumberValue, element.NumberValue, $"{elementNumberValue} not equal {element.NumberValue} with random seed {rndSeed}.");
                    }
                }

                // confirm end of stream
                Assert.AreEqual(0, reader.BytesLeft, "BytesLeft not zero as expected with random seed " + rndSeed);
                Assert.IsTrue(reader.EndOfStream, "Not end of stream as expected with random seed " + rndSeed);
            }
        }
    }
}