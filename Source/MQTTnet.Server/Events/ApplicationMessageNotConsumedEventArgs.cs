// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet.Server;

public sealed class ApplicationMessageNotConsumedEventArgs : EventArgs
{
    public ApplicationMessageNotConsumedEventArgs(MqttApplicationMessage applicationMessage, string senderId)
    {
        ApplicationMessage = applicationMessage ?? throw new ArgumentNullException(nameof(applicationMessage));
        SenderId = senderId;
    }

    /// <summary>
    ///     Gets the application message which was not consumed by any client.
    /// </summary>
    public MqttApplicationMessage ApplicationMessage { get; }

    /// <summary>
    ///     Gets the ID of the client which has sent the affected application message.
    /// </summary>
    public string SenderId { get; }
}