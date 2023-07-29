// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;

namespace MQTTnet.Tests.Clients
{
    [TestClass]
    public sealed class MqttClientOptionsBuilder_Tests
    {
        [TestMethod]
        public void WithConnectionUri_Credential_Test()
        {
            var options = new MqttClientOptionsBuilder()
                .WithConnectionUri("mqtt://user:password@127.0.0.1")
                .Build();
            
            Assert.AreEqual("user", options.Credentials.GetUserName(null));
            Assert.IsTrue(Encoding.UTF8.GetBytes("password").SequenceEqual(options.Credentials.GetPassword(null)));
        }
    }
}
