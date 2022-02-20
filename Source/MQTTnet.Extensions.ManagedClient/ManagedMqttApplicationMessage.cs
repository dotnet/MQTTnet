// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace MQTTnet.Extensions.ManagedClient
{
    public class ManagedMqttApplicationMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public MqttApplicationMessage ApplicationMessage { get; set; }
    }
}
