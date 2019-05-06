using System;
using System.Threading.Tasks;
using IronPython.Runtime;
using Microsoft.Extensions.Logging;
using MQTTnet.Server.Scripting;

namespace MQTTnet.Server.Mqtt
{
    public class MqttClientSubscribedTopicHandler : IMqttServerClientSubscribedTopicHandler
    {
        private readonly PythonScriptHostService _pythonScriptHostService;
        private readonly ILogger _logger;

        public MqttClientSubscribedTopicHandler(PythonScriptHostService pythonScriptHostService, ILogger<MqttClientSubscribedTopicHandler> logger)
        {
            _pythonScriptHostService = pythonScriptHostService ?? throw new ArgumentNullException(nameof(pythonScriptHostService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task HandleClientSubscribedTopicAsync(MqttServerClientSubscribedTopicEventArgs eventArgs)
        {
            try
            {
                var pythonEventArgs = new PythonDictionary
                {
                    { "client_id", eventArgs.ClientId },
                    { "topic", eventArgs.TopicFilter.Topic },
                    { "qos", (int)eventArgs.TopicFilter.QualityOfServiceLevel }
                };

                _pythonScriptHostService.InvokeOptionalFunction("on_client_subscribed_topic", pythonEventArgs);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while handling client subscribed topic event.");
            }

            return Task.CompletedTask;
        }
    }
}
