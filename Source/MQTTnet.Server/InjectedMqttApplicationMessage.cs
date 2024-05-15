// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace MQTTnet.Server;

public sealed class InjectedMqttApplicationMessage
{
    public InjectedMqttApplicationMessage(MqttApplicationMessage applicationMessage)
    {
        ApplicationMessage = applicationMessage ?? throw new ArgumentNullException(nameof(applicationMessage));
    }

    public MqttApplicationMessage ApplicationMessage { get; }

    /// <summary>
    ///     Gets or sets the session items which should be used for all event handlers which are involved in message
    ///     processing.
    ///     If _null_ is specified the singleton session items from the server are used instead.
    /// </summary>
    public IDictionary CustomSessionItems { get; set; }

    public string SenderClientId { get; set; } = string.Empty;
}