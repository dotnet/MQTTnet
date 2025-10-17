// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet.Server;

public sealed class ApplicationMessageEnqueuedEventArgs : EventArgs
{
    public ApplicationMessageEnqueuedEventArgs(string senderClientId, string receiverClientId, MqttApplicationMessage applicationMessage, bool isDropped)
    {
        SenderClientId = senderClientId ?? throw new ArgumentNullException( nameof(senderClientId));
        ReceiverClientId = receiverClientId ?? throw new ArgumentNullException(nameof(receiverClientId));
        ApplicationMessage = applicationMessage ?? throw new ArgumentNullException(nameof(applicationMessage));
        IsDropped = isDropped;
    }

    public string SenderClientId { get; }

    public string ReceiverClientId { get; }

    public bool IsDropped { get; }

    public MqttApplicationMessage ApplicationMessage { get; }
}