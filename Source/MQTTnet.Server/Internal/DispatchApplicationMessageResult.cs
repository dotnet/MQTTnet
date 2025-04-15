// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Packets;

namespace MQTTnet.Server.Internal;

public sealed class DispatchApplicationMessageResult
{
    public DispatchApplicationMessageResult(int reasonCode, bool closeConnection, string reasonString, List<MqttUserProperty> userProperties)
    {
        ReasonCode = reasonCode;
        CloseConnection = closeConnection;
        ReasonString = reasonString;
        UserProperties = userProperties;
    }

    public bool CloseConnection { get; }

    public int ReasonCode { get; }

    public string ReasonString { get; }

    public List<MqttUserProperty> UserProperties { get; }
}