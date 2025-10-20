// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet;

public sealed class MqttClientCredentials : IMqttClientCredentialsProvider
{
    readonly byte[]? _password;
    readonly string _userName;

    public MqttClientCredentials(string userName, byte[]? password = null)
    {
        _userName = userName;
        _password = password;
    }

    public byte[]? GetPassword(MqttClientOptions clientOptions)
    {
        return _password;
    }

    public string GetUserName(MqttClientOptions clientOptions)
    {
        return _userName;
    }
}