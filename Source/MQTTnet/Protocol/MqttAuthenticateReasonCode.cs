// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet.Protocol
{
    public enum MqttAuthenticateReasonCode
    {
        Success = 0,
        ContinueAuthentication = 24,
        ReAuthenticate = 25
    }
}
