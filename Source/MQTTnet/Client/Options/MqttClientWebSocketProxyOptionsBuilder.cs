// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;

namespace MQTTnet.Client;

public sealed class MqttClientWebSocketProxyOptionsBuilder
{
    readonly MqttClientWebSocketProxyOptions _proxyOptions = new();

    public MqttClientWebSocketProxyOptions Build()
    {
        return _proxyOptions;
    }

    public MqttClientWebSocketProxyOptionsBuilder WithAddress(string address)
    {
        _proxyOptions.Address = address;
        return this;
    }

    public MqttClientWebSocketProxyOptionsBuilder WithBypassList(string[] bypassList)
    {
        _proxyOptions.BypassList = bypassList;
        return this;
    }

    public MqttClientWebSocketProxyOptionsBuilder WithBypassList(IEnumerable<string> bypassList)
    {
        _proxyOptions.BypassList = bypassList?.ToArray();
        return this;
    }

    public MqttClientWebSocketProxyOptionsBuilder WithBypassOnLocal(bool bypassOnLocal = true)
    {
        _proxyOptions.BypassOnLocal = bypassOnLocal;
        return this;
    }

    public MqttClientWebSocketProxyOptionsBuilder WithDomain(string domain)
    {
        _proxyOptions.Domain = domain;
        return this;
    }

    public MqttClientWebSocketProxyOptionsBuilder WithPassword(string password)
    {
        _proxyOptions.Password = password;
        return this;
    }

    public MqttClientWebSocketProxyOptionsBuilder WithUseDefaultCredentials(bool useDefaultCredentials = true)
    {
        _proxyOptions.UseDefaultCredentials = useDefaultCredentials;
        return this;
    }

    public MqttClientWebSocketProxyOptionsBuilder WithUsername(string username)
    {
        _proxyOptions.Username = username;
        return this;
    }
}