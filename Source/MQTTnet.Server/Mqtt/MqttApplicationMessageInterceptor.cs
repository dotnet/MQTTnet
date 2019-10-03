using System;
using System.Threading.Tasks;
using IronPython.Runtime;
using Microsoft.Extensions.Logging;
using MQTTnet.Protocol;
using MQTTnet.Server.Scripting;

namespace MQTTnet.Server.Mqtt
{
    public class MqttApplicationMessageInterceptor : IMqttServerApplicationMessageInterceptor
    {
        private readonly PythonScriptHostService _pythonScriptHostService;
        private readonly ILogger _logger;

        public MqttApplicationMessageInterceptor(PythonScriptHostService pythonScriptHostService, ILogger<MqttApplicationMessageInterceptor> logger)
        {
            _pythonScriptHostService = pythonScriptHostService ?? throw new ArgumentNullException(nameof(pythonScriptHostService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task InterceptApplicationMessagePublishAsync(MqttApplicationMessageInterceptorContext context)
        {
            try
            {
                // This might be not set when a message was published by the server instead of a client.
                context.SessionItems.TryGetValue(MqttServerConnectionValidator.WrappedSessionItemsKey, out var sessionItems);

                var pythonContext = new PythonDictionary
                {
                    { "client_id", context.ClientId },
                    { "session_items", sessionItems },
                    { "retain", context.ApplicationMessage.Retain },
                    { "accept_publish", context.AcceptPublish },
                    { "close_connection", context.CloseConnection },
                    { "topic", context.ApplicationMessage.Topic },
                    { "qos", (int)context.ApplicationMessage.QualityOfServiceLevel }
                };
                
                _pythonScriptHostService.InvokeOptionalFunction("on_intercept_application_message", pythonContext);

                context.AcceptPublish = (bool)pythonContext.get("accept_publish", context.AcceptPublish);
                context.CloseConnection = (bool)pythonContext.get("close_connection", context.CloseConnection);
                context.ApplicationMessage.Topic = (string)pythonContext.get("topic", context.ApplicationMessage.Topic);
                context.ApplicationMessage.QualityOfServiceLevel = (MqttQualityOfServiceLevel)(int)pythonContext.get("qos", (int)context.ApplicationMessage.QualityOfServiceLevel);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while intercepting application message.");
            }

            return Task.CompletedTask;
        }
    }
}