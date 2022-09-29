// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Internal;

namespace MQTTnet.Extensions.ManagedClient
{
    public sealed class ApplicationMessagesManager
    {
        readonly MqttNetSourceLogger _logger;

        readonly Queue<ManagedMqttApplicationMessage> _messageQueue = new Queue<ManagedMqttApplicationMessage>();
        readonly IMqttClient _mqttClient;

        public ApplicationMessagesManager(IMqttClient mqttClient, IMqttNetLogger logger)
        {
            _mqttClient = mqttClient ?? throw new ArgumentNullException(nameof(mqttClient));

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _logger = logger.WithSource<ApplicationMessagesManager>();
        }

        public AsyncEvent<ApplicationMessageDroppedEventArgs> ApplicationMessageDroppedEvent { get; } = new AsyncEvent<ApplicationMessageDroppedEventArgs>();

        public AsyncEvent<ApplicationMessageEnqueueingEventArgs> ApplicationMessageEnqueueingEvent { get; } = new AsyncEvent<ApplicationMessageEnqueueingEventArgs>();

        public int EnqueuedApplicationMessagesCount
        {
            get
            {
                lock (_messageQueue)
                {
                    return _messageQueue.Count;
                }
            }
        }

        public void Dequeue()
        {
            lock (_messageQueue)
            {
                _messageQueue.Dequeue();
            }
        }

        public async Task Enqueue(ManagedMqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null)
            {
                throw new ArgumentNullException(nameof(applicationMessage));
            }

            // Fire the event first so that the handler can make sure that this message is 
            // properly backed up in a suitable storage.
            if (ApplicationMessageEnqueueingEvent.HasHandlers)
            {
                var eventArgs = new ApplicationMessageEnqueueingEventArgs(applicationMessage, 0);
                await ApplicationMessageEnqueueingEvent.InvokeAsync(eventArgs).ConfigureAwait(false);
            }

            lock (_messageQueue)
            {
                
                // TODO: Drop if too much!
                
                _messageQueue.Enqueue(applicationMessage);
            }
        }

        public bool TryPeek(out ManagedMqttApplicationMessage applicationMessage)
        {
            lock (_messageQueue)
            {
                if (_messageQueue.Count == 0)
                {
                    applicationMessage = null;
                    return false;
                }

                applicationMessage = _messageQueue.Peek();
                return true;
            }
        }
    }
}