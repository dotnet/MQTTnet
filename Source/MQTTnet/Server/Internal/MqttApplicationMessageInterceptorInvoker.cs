using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Server.Internal
{
    public sealed class MqttApplicationMessageInterceptorInvoker
    {
        readonly IMqttServerOptions _serverOptions;
        readonly string _clientId;
        readonly IDictionary<object, object> _sessionItems;
        
        public MqttApplicationMessageInterceptorInvoker(IMqttServerOptions serverOptions, string clientId, IDictionary<object, object> sessionItems)
        {
            _serverOptions = serverOptions ?? throw new ArgumentNullException(nameof(serverOptions));
            _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            _sessionItems = sessionItems ?? throw new ArgumentNullException(nameof(sessionItems));
        }

        public MqttApplicationMessage ApplicationMessage { get; private set; }
        
        public MqttApplicationMessageResponse Response { get; private set; } = new MqttApplicationMessageResponse();
            
        public bool ProcessPublish { get; private set; } = true;

        public bool CloseConnection { get; private set; }
            
        public async Task Invoke(MqttApplicationMessage applicationMessage, CancellationToken cancellationToken)
        {
            // Reset to defaults.
            Response = new MqttApplicationMessageResponse();
            ProcessPublish = true;
            CloseConnection = false;
            ApplicationMessage = applicationMessage;
                
            // Intercept.
            var interceptor = _serverOptions.ApplicationMessageInterceptor;
            if (interceptor == null)
            {
                return;
            }
            
            var interceptorContext = new MqttApplicationMessageInterceptorContext
            {
                ClientId = _clientId,
                ApplicationMessage = applicationMessage,
                SessionItems = _sessionItems,
                ProcessPublish = true,
                CloseConnection = false,
                CancellationToken = cancellationToken
            };

            await interceptor.InterceptApplicationMessagePublishAsync(interceptorContext).ConfigureAwait(false);

            // Expose results.
            ProcessPublish = interceptorContext.ProcessPublish && interceptorContext.ApplicationMessage != null;
            CloseConnection = interceptorContext.CloseConnection;
            Response = interceptorContext.Response;
            ApplicationMessage = interceptorContext.ApplicationMessage;
        }
    }
}