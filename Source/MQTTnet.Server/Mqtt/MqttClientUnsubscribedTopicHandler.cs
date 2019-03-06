using System;
using System.Threading.Tasks;
using IronPython.Runtime;
using Microsoft.Extensions.Logging;
using MQTTnet.Server.Scripting;

namespace MQTTnet.Server.Mqtt
{
    public class MqttClientUnsubscribedTopicHandler : IMqttServerClientUnsubscribedTopicHandler
    {
        private readonly PythonScriptHostService _pythonScriptHostService;
        private readonly ILogger _logger;

        public MqttClientUnsubscribedTopicHandler(PythonScriptHostService pythonScriptHostService, ILogger<MqttClientUnsubscribedTopicHandler> logger)
        {
            _pythonScriptHostService = pythonScriptHostService ?? throw new ArgumentNullException(nameof(pythonScriptHostService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task HandleClientUnsubscribedTopicAsync(MqttServerClientUnsubscribedTopicEventArgs eventArgs)
        {
            try
            {
                var pythonEventArgs = new PythonDictionary
                {
                    { "client_id", eventArgs.ClientId },
                    { "topic", eventArgs.TopicFilter }
                };

                _pythonScriptHostService.InvokeOptionalFunction("on_client_unsubscribed_topic", pythonEventArgs);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while handling client unsubscribed topic event.");
            }

            return Task.CompletedTask;
        }
    }
}
