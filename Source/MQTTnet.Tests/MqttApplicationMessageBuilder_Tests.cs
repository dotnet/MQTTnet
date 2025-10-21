// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
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
}