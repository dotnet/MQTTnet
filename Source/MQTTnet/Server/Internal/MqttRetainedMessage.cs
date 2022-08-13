// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace MQTTnet.Server
{
    public sealed class MqttRetainedMessage
    {
        public MqttApplicationMessage ApplicationMessage { get; set; }

        /// <summary>
        ///     Gets the timestamp in UTC when this retained message eas published.
        /// </summary>
        public DateTime PublishedTimestamp { get; set; }
    }
}