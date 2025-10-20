// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet.Server;

public sealed class RetainedMessageChangedEventArgs : EventArgs
{
    public RetainedMessageChangedEventArgs(string clientId, MqttApplicationMessage changedRetainedMessage, List<MqttApplicationMessage> storedRetainedMessages)
    {
        ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
        ChangedRetainedMessage = changedRetainedMessage ?? throw new ArgumentNullException(nameof(changedRetainedMessage));
        StoredRetainedMessages = storedRetainedMessages ?? throw new ArgumentNullException(nameof(storedRetainedMessages));
    }

    public MqttApplicationMessage ChangedRetainedMessage { get; }

    public string ClientId { get; }

    public List<MqttApplicationMessage> StoredRetainedMessages { get; }
}