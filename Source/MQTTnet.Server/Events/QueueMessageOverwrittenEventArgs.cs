// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using MQTTnet.Packets;

namespace MQTTnet.Server
{
    public sealed class QueueMessageOverwrittenEventArgs : EventArgs
    {
        public QueueMessageOverwrittenEventArgs(string receiverClientId, MqttPacket packet)
        {
            ReceiverClientId = receiverClientId ?? throw new ArgumentNullException(nameof(receiverClientId));
            Packet = packet ?? throw new ArgumentNullException(nameof(packet));
        }

        public MqttPacket Packet { get; }

        public string ReceiverClientId { get; }
    }
}