// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace MQTTnet.Server
{
    public sealed class RetainedMessageChangedEventArgs : EventArgs
    {
        public RetainedMessageChangedEventArgs(
            string clientId,
            MqttRetainedMessage changedMessage,
            RetainedMessageChangeType changeType,
            List<MqttRetainedMessage> allMessages)
        {
            ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            ChangedRetainedMessage = changedMessage ?? throw new ArgumentNullException(nameof(changedMessage));
            ChangeType = changeType;
            StoredRetainedMessages = allMessages ?? throw new ArgumentNullException(nameof(allMessages));
        }

        public RetainedMessageChangeType ChangeType { get; }

        public MqttRetainedMessage ChangedRetainedMessage { get; }

        public string ClientId { get; }

        public List<MqttRetainedMessage> StoredRetainedMessages { get; }
    }
}