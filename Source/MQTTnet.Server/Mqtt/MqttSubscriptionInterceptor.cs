using System;
using System.Threading.Tasks;
using IronPython.Runtime;
using Microsoft.Extensions.Logging;
using MQTTnet.Server.Scripting;

namespace MQTTnet.Server.Mqtt
{
    public class MqttSubscriptionInterceptor : IMqttServerSubscriptionInterceptor
    {
        private readonly PythonScriptHostService _pythonScriptHostService;
        private readonly ILogger _logger;

        public MqttSubscriptionInterceptor(PythonScriptHostService pythonScriptHostService, ILogger<MqttSubscriptionInterceptor> logger)
        {
            _pythonScriptHostService = pythonScriptHostService ?? throw new ArgumentNullException(nameof(pythonScriptHostService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task InterceptSubscriptionAsync(MqttSubscriptionInterceptorContext context)
        {
            try
            {
                var sessionItems = (PythonDictionary)context.SessionItems[MqttServerConnectionValidator.WrappedSessionItemsKey];

                var pythonContext = new PythonDictionary
                {
                    { "client_id", context.ClientId },
                    { "session_items", sessionItems },
                    { "topic", context.TopicFilter.Topic },
                    { "qos", (int)context.TopicFilter.QualityOfServiceLevel },
                    { "accept_subscription", context.AcceptSubscription },
                    { "close_connection", context.CloseConnection }
                };

                _pythonScriptHostService.InvokeOptionalFunction("on_intercept_subscription", pythonContext);

                context.AcceptSubscription = (bool)pythonContext["accept_subscription"];
                context.CloseConnection = (bool)pythonContext["close_connection"];
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while intercepting subscription.");
            }

            return Task.CompletedTask;
        }
    }
}
