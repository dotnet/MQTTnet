using System;
using System.Threading.Tasks;
using IronPython.Runtime;
using Microsoft.Extensions.Logging;
using MQTTnet.Server.Scripting;

namespace MQTTnet.Server.Mqtt
{
    public class MqttUnsubscriptionInterceptor : IMqttServerUnsubscriptionInterceptor
    {
        private readonly PythonScriptHostService _pythonScriptHostService;
        private readonly ILogger _logger;

        public MqttUnsubscriptionInterceptor(PythonScriptHostService pythonScriptHostService, ILogger<MqttUnsubscriptionInterceptor> logger)
        {
            _pythonScriptHostService = pythonScriptHostService ?? throw new ArgumentNullException(nameof(pythonScriptHostService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task InterceptUnsubscriptionAsync(MqttUnsubscriptionInterceptorContext context)
        {
            try
            {
                var sessionItems = (PythonDictionary)context.SessionItems[MqttServerConnectionValidator.WrappedSessionItemsKey];

                var pythonContext = new PythonDictionary
                {
                    { "client_id", context.ClientId },
                    { "session_items", sessionItems },
                    { "topic", context.Topic },
                    { "accept_unsubscription", context.AcceptUnsubscription },
                    { "close_connection", context.CloseConnection }
                };

                _pythonScriptHostService.InvokeOptionalFunction("on_intercept_unsubscription", pythonContext);

                context.AcceptUnsubscription = (bool)pythonContext["accept_unsubscription"];
                context.CloseConnection = (bool)pythonContext["close_connection"];
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while intercepting unsubscription.");
            }

            return Task.CompletedTask;
        }
    }
}
