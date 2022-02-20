// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet.Client
{
    public sealed class MqttClientCredentials : IMqttClientCredentialsProvider
    {
        readonly string _userName;
        readonly byte[] _password;

        public MqttClientCredentials(string userName, byte[] password = null)
        {
            _userName = userName;
            _password = password;
        }
        
        public string GetUserName(MqttClientOptions clientOptions)
        {
            return _userName;
        }

        public byte[] GetPassword(MqttClientOptions clientOptions)
        {
            return _password;
        }
    }
}
