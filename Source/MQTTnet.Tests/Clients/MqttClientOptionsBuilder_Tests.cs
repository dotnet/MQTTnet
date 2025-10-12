// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MQTTnet.Tests.Clients;

// ReSharper disable InconsistentNaming
[TestClass]
public class MqttClientOptionsBuilder_Tests
{
    [TestMethod]
    public void WithConnectionUri_Credential_Test()
    {
        var options = new MqttClientOptionsBuilder()
            .WithConnectionUri("mqtt://user:password@127.0.0.1")
            .Build();

        Assert.AreEqual("user", options.Credentials.GetUserName(null));
        Assert.IsTrue("password"u8.ToArray().SequenceEqual(options.Credentials.GetPassword(null)));
    }
}