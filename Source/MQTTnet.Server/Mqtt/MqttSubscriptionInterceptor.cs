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
        private readonly ILogger<MqttSubscriptionInterceptor> _logger;

        public MqttSubscriptionInterceptor(PythonScriptHostService pythonScriptHostService, ILogger<MqttSubscriptionInterceptor> logger)
        {
            _pythonScriptHostService = pythonScriptHostService ?? throw new ArgumentNullException(nameof(pythonScriptHostService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task InterceptSubscriptionAsync(MqttSubscriptionInterceptorContext context)
        {
            try
            {
                var pythonContext = new PythonDictionary
                {
                    { "accept_subscription", context.AcceptSubscription },
                    { "close_connection", context.CloseConnection },

                    { "client_id", context.ClientId },
                    { "topic", context.TopicFilter.Topic },
                    { "qos", (int)context.TopicFilter.QualityOfServiceLevel }
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
