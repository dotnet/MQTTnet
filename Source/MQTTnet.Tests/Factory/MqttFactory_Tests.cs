// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Server;

namespace MQTTnet.Tests.Factory;

// ReSharper disable InconsistentNaming
[TestClass]
public sealed class MqttFactory_Tests : BaseTestClass
{
    [TestMethod]
    public void Create_ApplicationMessageBuilder()
    {
        var factory = new MqttClientFactory();
        var builder = factory.CreateApplicationMessageBuilder();

        Assert.IsNotNull(builder);
    }

    [TestMethod]
    public void Create_ClientOptionsBuilder()
    {
        var factory = new MqttClientFactory();
        var builder = factory.CreateClientOptionsBuilder();

        Assert.IsNotNull(builder);
    }

    [TestMethod]
    public void Create_ServerOptionsBuilder()
    {
        var factory = new MqttServerFactory();
        var builder = factory.CreateServerOptionsBuilder();

        Assert.IsNotNull(builder);
    }

    [TestMethod]
    public void Create_SubscribeOptionsBuilder()
    {
        var factory = new MqttClientFactory();
        var builder = factory.CreateSubscribeOptionsBuilder();

        Assert.IsNotNull(builder);
    }

    [TestMethod]
    public void Create_UnsubscribeOptionsBuilder()
    {
        var factory = new MqttClientFactory();
        var builder = factory.CreateUnsubscribeOptionsBuilder();

        Assert.IsNotNull(builder);
    }

    [TestMethod]
    public void Create_TopicFilterBuilder()
    {
        var factory = new MqttClientFactory();
        var builder = factory.CreateTopicFilterBuilder();

        Assert.IsNotNull(builder);
    }

    [TestMethod]
    public void Create_MqttServer()
    {
        var factory = new MqttServerFactory();
        var server = factory.CreateMqttServer(new MqttServerOptionsBuilder().Build());

        Assert.IsNotNull(server);
    }

    [TestMethod]
    public void Create_MqttClient()
    {
        var factory = new MqttClientFactory();
        var client = factory.CreateMqttClient();

        Assert.IsNotNull(client);
    }

    [TestMethod]
    public void Create_LowLevelMqttClient()
    {
        var factory = new MqttClientFactory();
        var client = factory.CreateLowLevelMqttClient();

        Assert.IsNotNull(client);
    }
}