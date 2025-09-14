// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Formatter;
using MQTTnet.Internal;

namespace MQTTnet.Tests.Diagnostics;

// ReSharper disable InconsistentNaming
[TestClass]
public sealed class PacketInspection_Tests : BaseTestClass
{
    [TestMethod]
    public async Task Inspect_Client_Packets()
    {
        using var testEnvironment = CreateTestEnvironment();
        await testEnvironment.StartServer();

        using var mqttClient = testEnvironment.CreateClient();
        var mqttClientOptions = testEnvironment.ClientFactory.CreateClientOptionsBuilder()
            .WithClientId("CLIENT_ID") // Must be fixed.
            .WithProtocolVersion(MqttProtocolVersion.V311)
            .WithTcpServer("127.0.0.1", testEnvironment.ServerPort)
            .Build();

        var packets = new List<string>();

        mqttClient.InspectPacketAsync += eventArgs =>
        {
            packets.Add(eventArgs.Direction + ":" + Convert.ToBase64String(eventArgs.Buffer));
            return CompletedTask.Instance;
        };

        await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

        Assert.AreEqual(2, packets.Count);
        Assert.AreEqual("Outbound:ECwABE1RVFQEAgAPACBJbnNwZWN0X0NsaWVudF9QYWNrZXRzX0NMSUVOVF9JRA==", packets[0]); // CONNECT
        Assert.AreEqual("Inbound:IAIAAA==", packets[1]); // CONNACK
    }
}