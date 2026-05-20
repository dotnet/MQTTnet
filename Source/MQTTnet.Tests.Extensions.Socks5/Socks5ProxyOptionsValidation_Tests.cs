// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using MQTTnet.Extensions.Socks5;

namespace MQTTnet.Tests.Extensions.Socks5;

[TestClass]
public sealed class Socks5ProxyOptionsValidation_Tests
{
    [TestMethod]
    public void Builder_Build_Without_Host_Throws()
    {
        var builder = new Socks5ProxyOptionsBuilder();

        Assert.ThrowsExactly<InvalidOperationException>(() => builder.Build());
    }

    [TestMethod]
    public void Builder_Build_With_Invalid_Port_Throws()
    {
        var builder = new Socks5ProxyOptionsBuilder()
            .WithHost("127.0.0.1")
            .WithPort(0);

        Assert.ThrowsExactly<InvalidOperationException>(() => builder.Build());
    }

    [TestMethod]
    public void Builder_Build_With_NonPositive_HandshakeTimeout_Throws()
    {
        var builder = new Socks5ProxyOptionsBuilder()
            .WithHost("127.0.0.1")
            .WithHandshakeTimeout(TimeSpan.Zero);

        Assert.ThrowsExactly<InvalidOperationException>(() => builder.Build());
    }

    [TestMethod]
    public void Builder_Build_With_Password_But_Without_Username_Throws()
    {
        var builder = new Socks5ProxyOptionsBuilder()
            .WithHost("127.0.0.1")
            .WithCredentials(null, new byte[] { 0x01, 0x02 });

        Assert.ThrowsExactly<InvalidOperationException>(() => builder.Build());
    }

    [TestMethod]
    public void StreamProvider_Ctor_With_Invalid_Port_Throws()
    {
        var options = new Socks5ProxyOptions
        {
            Host = "127.0.0.1",
            Port = 70000
        };

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = new Socks5StreamProvider(options));
    }

    [TestMethod]
    public void StreamProvider_Ctor_With_NonPositive_HandshakeTimeout_Throws()
    {
        var options = new Socks5ProxyOptions
        {
            Host = "127.0.0.1",
            Port = 1080,
            HandshakeTimeout = TimeSpan.Zero
        };

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = new Socks5StreamProvider(options));
    }

    [TestMethod]
    public void StreamProvider_Ctor_With_Password_But_Without_Username_Throws()
    {
        var options = new Socks5ProxyOptions
        {
            Host = "127.0.0.1",
            Port = 1080,
            Password = new byte[] { 0x01 }
        };

        Assert.ThrowsExactly<ArgumentException>(() => _ = new Socks5StreamProvider(options));
    }
}
