// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace MQTTnet.Server;

public sealed class InterceptingPublishEventArgs : EventArgs
{
    public InterceptingPublishEventArgs(MqttApplicationMessage applicationMessage, string clientId, string userName, IDictionary sessionItems, CancellationToken cancellationToken)
    {
        ApplicationMessage = applicationMessage ?? throw new ArgumentNullException(nameof(applicationMessage));
        CancellationToken = cancellationToken;
        ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
        UserName = userName;
        SessionItems = sessionItems ?? throw new ArgumentNullException(nameof(sessionItems));
    }

    public MqttApplicationMessage ApplicationMessage { get; set; }

    /// <summary>
    ///     Gets the cancellation token which can indicate that the client connection gets down.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    ///     Gets the client identifier.
    ///     Hint: This identifier needs to be unique over all used clients / devices on the broker to avoid connection issues.
    /// </summary>
    public string ClientId { get; }

    public bool CloseConnection { get; set; }

    /// <summary>
    ///     Gets or sets whether the publish should be processed internally.
    /// </summary>
    public bool ProcessPublish { get; set; } = true;

    /// <summary>
    ///     Gets the response which will be sent to the client via the PUBACK etc. packets.
    /// </summary>
    public PublishResponse Response { get; } = new();

    /// <summary>
    ///     Gets or sets a key/value collection that can be used to share data within the scope of this session.
    /// </summary>
    public IDictionary SessionItems { get; }

    /// <summary>
    ///     Gets the user name of the client.
    /// </summary>
    public string UserName { get; }
}