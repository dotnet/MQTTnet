// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet.Server.Exceptions;

public class MqttPendingMessagesOverflowException : Exception
{
    public MqttPendingMessagesOverflowException(string sessionId, MqttPendingMessagesOverflowStrategy overflowStrategy) : base(
        $"Send buffer max pending messages overflow occurred for session '{sessionId}'. Strategy: {overflowStrategy}.")
    {
        SessionId = sessionId;
        OverflowStrategy = overflowStrategy;
    }

    public MqttPendingMessagesOverflowStrategy OverflowStrategy { get; }

    public string SessionId { get; }
}