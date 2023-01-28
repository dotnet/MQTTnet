// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Server
{
    public interface IMqttRetainedMessagesManager
    {
        Task ClearMessages();

        Task<IList<MqttApplicationMessage>> GetMessages(CancellationToken cancellationToken = default);

        Task LoadMessages(IEnumerable<SubscriptionRetainedMessagesResult> subscriptions, CancellationToken cancellationToken = default);

        Task Start();

        Task Stop();

        Task UpdateMessage(string clientId, MqttApplicationMessage applicationMessage);
    }
}