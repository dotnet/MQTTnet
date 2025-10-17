// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet.Server;

public sealed class InterceptingClientApplicationMessageEnqueueEventArgs : EventArgs
{
    public InterceptingClientApplicationMessageEnqueueEventArgs(string senderClientId, string receiverClientId, MqttApplicationMessage applicationMessage)
    {
        SenderClientId = senderClientId ?? throw new ArgumentNullException(nameof(senderClientId));
        ReceiverClientId = receiverClientId ?? throw new ArgumentNullException(nameof(receiverClientId));
        ApplicationMessage = applicationMessage ?? throw new ArgumentNullException(nameof(applicationMessage));
    }

    /// <summary>
    ///     Gets or sets whether the enqueue of the application message should be performed or not.
    ///     If set to _False_ the client will not receive the application message.
    /// </summary>
    public bool AcceptEnqueue { get; set; } = true;

    public MqttApplicationMessage ApplicationMessage { get; }

    /// <summary>
    ///     Indicates if the connection with the sender should be closed.
    /// </summary>
    public bool CloseSenderConnection { get; set; }

    public string ReceiverClientId { get; }

    public string SenderClientId { get; }
}