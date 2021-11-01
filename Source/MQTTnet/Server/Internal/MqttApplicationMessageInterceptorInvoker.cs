using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Server
{
    public sealed class MqttApplicationMessageInterceptorInvoker
    {
        readonly MqttServerEventContainer _eventContainer;
        readonly string _clientId;
        readonly IDictionary<object, object> _sessionItems;
        
        public MqttApplicationMessageInterceptorInvoker(MqttServerEventContainer eventContainer, string clientId, IDictionary<object, object> sessionItems)
        {
            _eventContainer = eventContainer ?? throw new ArgumentNullException(nameof(eventContainer));
            _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            _sessionItems = sessionItems ?? throw new ArgumentNullException(nameof(sessionItems));
        }

        public MqttApplicationMessage ApplicationMessage { get; private set; }
        
        public PublishResponse Response { get; private set; } = new PublishResponse();
            
        public bool ProcessPublish { get; private set; } = true;

        public bool CloseConnection { get; private set; }
            
        public async Task Invoke(MqttApplicationMessage applicationMessage, CancellationToken cancellationToken)
        {
            // Reset to defaults.
            Response = new PublishResponse();
            ProcessPublish = true;
            CloseConnection = false;
            ApplicationMessage = applicationMessage;
                
            // Intercept.
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
                eventArgs.Response.ReasonCode = MqttApplicationMessageResponseReasonCode.TopicNameInvalid;
                eventArgs.ProcessPublish = false;
            }
            
            await _eventContainer.InterceptingPublishEvent.InvokeAsync(eventArgs).ConfigureAwait(false);

            // Expose results.
            ProcessPublish = eventArgs.ProcessPublish && eventArgs.ApplicationMessage != null;
            CloseConnection = eventArgs.CloseConnection;
            Response = eventArgs.Response;
            ApplicationMessage = eventArgs.ApplicationMessage;
        }
    }
}