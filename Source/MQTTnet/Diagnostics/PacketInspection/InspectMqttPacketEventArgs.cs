// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;

namespace MQTTnet.Diagnostics
{
    public sealed class InspectMqttPacketEventArgs : EventArgs
    {
        public InspectMqttPacketEventArgs(MqttPacketFlowDirection direction, ReadOnlySequence<byte> buffer)
        {
            Direction = direction;
            Buffer = buffer;
        }

        public ReadOnlySequence<byte> Buffer { get; }

        public MqttPacketFlowDirection Direction { get; }
    }
}