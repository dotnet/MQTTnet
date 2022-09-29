// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace MQTTnet.Extensions.ManagedClient
{
    public sealed class ApplicationMessageEnqueueingEventArgs : EventArgs
    {
        public ApplicationMessageEnqueueingEventArgs(ManagedMqttApplicationMessage applicationMessage, ushort packetIdentifier)
        {
            ApplicationMessage = applicationMessage ?? throw new ArgumentNullException(nameof(applicationMessage));
            PacketIdentifier = packetIdentifier;
        }

        public ManagedMqttApplicationMessage ApplicationMessage { get; }

        public ushort PacketIdentifier { get; }
    }
}