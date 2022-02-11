// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public sealed class MqttApplicationMessageInterceptorInvoker
    {
        readonly string _clientId;
        readonly MqttServerEventContainer _eventContainer;
        readonly IDictionary _sessionItems;

        public MqttApplicationMessageInterceptorInvoker(MqttServerEventContainer eventContainer, string clientId, IDictionary sessionItems)
        {
            _eventContainer = eventContainer ?? throw new ArgumentNullException(nameof(eventContainer));
            _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            _sessionItems = sessionItems ?? throw new ArgumentNullException(nameof(sessionItems));
        }

        public async Task<InterceptingPublishEventArgs> Invoke(MqttApplicationMessage applicationMessage, CancellationToken cancellationToken)
        {
            var eventArgs = new InterceptingPublishEventArgs
            {
                ClientId = _clientId,
                ApplicationMessage = applicationMessage,
                SessionItems = _sessionItems,
                ProcessPublish = true,
                CloseConnection = false,
                CancellationToken = cancellationToken
            };

            if (string.IsNullOrEmpty(eventArgs.ApplicationMessage.Topic))
            {
                // This can happen if a topic alias us used but the topic is
                // unknown to the server.
                eventArgs.Response.ReasonCode = MqttPubAckReasonCode.TopicNameInvalid;
                eventArgs.ProcessPublish = false;
            }

            await _eventContainer.InterceptingPublishEvent.InvokeAsync(eventArgs).ConfigureAwait(false);

            return eventArgs;
        }
    }
}