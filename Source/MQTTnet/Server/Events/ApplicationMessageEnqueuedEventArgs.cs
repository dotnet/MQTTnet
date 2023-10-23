// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace MQTTnet.Server
{
    public sealed class ApplicationMessageEnqueuedEventArgs : EventArgs
    {
        public ApplicationMessageEnqueuedEventArgs(string senderClientId, string receiverClientId, MqttApplicationMessage applicationMessage, bool dropped)
        {
            SenderClientId = senderClientId ?? throw new ArgumentNullException( nameof(senderClientId));
            ReceiverClientId = receiverClientId ?? throw new ArgumentNullException(nameof(receiverClientId));
            DroppedQueueFull = dropped;
            ApplicationMessage = applicationMessage ?? throw new ArgumentNullException(nameof(applicationMessage));
        }
        public string SenderClientId { get; }

        public string ReceiverClientId { get; }

        public bool DroppedQueueFull { get; }


        public MqttApplicationMessage ApplicationMessage { get; }
    }
}