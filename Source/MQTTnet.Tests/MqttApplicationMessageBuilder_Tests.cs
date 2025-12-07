// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using MQTTnet.Formatter;
using MQTTnet.Formatter.V5;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Tests;

// ReSharper disable InconsistentNaming
[TestClass]
public sealed class MqttApplicationMessageBuilder_Tests
{
    [TestMethod]
    public void CreateApplicationMessage_TopicOnly()
    {
        var message = new MqttApplicationMessageBuilder().WithTopic("Abc").Build();
        Assert.AreEqual("Abc", message.Topic);
        Assert.IsFalse(message.Retain);
        Assert.AreEqual(MqttQualityOfServiceLevel.AtMostOnce, message.QualityOfServiceLevel);
    }

    [TestMethod]
    public void CreateApplicationMessage_TimeStampPayload()
    {
        var message = new MqttApplicationMessageBuilder().WithTopic("xyz").WithPayload(TimeSpan.FromSeconds(360).ToString()).Build();
        Assert.AreEqual("xyz", message.Topic);
        Assert.IsFalse(message.Retain);
        Assert.AreEqual(MqttQualityOfServiceLevel.AtMostOnce, message.QualityOfServiceLevel);
        Assert.AreEqual("00:06:00", Encoding.UTF8.GetString(message.Payload));
    }

    [TestMethod]
    public void CreateApplicationMessage_StreamPayload()
    {
        var stream = new MemoryStream("xHello"u8.ToArray()) { Position = 1 };

        var message = new MqttApplicationMessageBuilder().WithTopic("123").WithPayload(stream).Build();
        Assert.AreEqual("123", message.Topic);
        Assert.IsFalse(message.Retain);
        Assert.AreEqual(MqttQualityOfServiceLevel.AtMostOnce, message.QualityOfServiceLevel);
        Assert.AreEqual("Hello", Encoding.UTF8.GetString(message.Payload));
    }

    [TestMethod]
    public void CreateApplicationMessage_Retained()
    {
        var message = new MqttApplicationMessageBuilder().WithTopic("lol").WithRetainFlag().Build();
        Assert.AreEqual("lol", message.Topic);
        Assert.IsTrue(message.Retain);
        Assert.AreEqual(MqttQualityOfServiceLevel.AtMostOnce, message.QualityOfServiceLevel);
    }

    [TestMethod]
    public void CreateApplicationMessage_QosLevel2()
    {
        var message = new MqttApplicationMessageBuilder().WithTopic("rofl").WithRetainFlag().WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce).Build();
        Assert.AreEqual("rofl", message.Topic);
        Assert.IsTrue(message.Retain);
        Assert.AreEqual(MqttQualityOfServiceLevel.ExactlyOnce, message.QualityOfServiceLevel);
    }

    [TestMethod]
    public void CreateApplicationMessage_UserProperty_ReadOnlyMemoryValue()
    {
        var value = "utf8";
        var buffer = Encoding.UTF8.GetBytes(value);

        var message = new MqttApplicationMessageBuilder().WithTopic("topic").WithUserProperty("name", buffer.AsMemory()).Build();

        Assert.IsNotNull(message.UserProperties);
        Assert.HasCount(1, message.UserProperties);

        var userProperty = message.UserProperties[0];
        CollectionAssert.AreEqual(buffer, userProperty.ValueBuffer.ToArray());
        Assert.AreEqual(value, userProperty.Value);
    }

    [TestMethod]
    public void CreateApplicationMessage_UserProperty_ArraySegmentValue()
    {
        var buffer = Encoding.UTF8.GetBytes("segment");
        var segment = new ArraySegment<byte>(buffer);

        var message = new MqttApplicationMessageBuilder().WithTopic("topic").WithUserProperty("name", segment).Build();

        Assert.IsNotNull(message.UserProperties);
        Assert.HasCount(1, message.UserProperties);

        var userProperty = message.UserProperties[0];
        CollectionAssert.AreEqual(buffer, userProperty.ValueBuffer.ToArray());
        Assert.AreEqual("segment", userProperty.Value);
    }

    [TestMethod]
    public void WriteUserProperty_FromBinaryBuffer_EqualsStringEncoding()
    {
        var name = "name";
        var value = "value";
        var encoded = Encoding.UTF8.GetBytes(value);

        var binaryWriter = new MqttBufferWriter(32, 256);
        var binaryPropertiesWriter = new MqttV5PropertiesWriter(binaryWriter);
        binaryPropertiesWriter.WriteUserProperties(new List<MqttUserProperty> { new(name, encoded.AsMemory()) });

        var stringWriter = new MqttBufferWriter(32, 256);
        var stringPropertiesWriter = new MqttV5PropertiesWriter(stringWriter);
        stringPropertiesWriter.WriteUserProperties(new List<MqttUserProperty> { new(name, value) });

        CollectionAssert.AreEqual(GetWrittenBytes(stringWriter), GetWrittenBytes(binaryWriter));
    }

    static byte[] GetWrittenBytes(MqttBufferWriter writer)
    {
        var length = writer.Length;
        var copy = new byte[length];
        Array.Copy(writer.GetBuffer(), copy, length);
        return copy;
    }
}