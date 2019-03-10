using System;
using System.Threading.Tasks;
using IronPython.Runtime;
using Microsoft.Extensions.Logging;
using MQTTnet.Server.Scripting;

namespace MQTTnet.Server.Mqtt
{
    public class MqttClientDisconnectedHandler : IMqttServerClientDisconnectedHandler
    {
        private readonly PythonScriptHostService _pythonScriptHostService;
        private readonly ILogger _logger;

        public MqttClientDisconnectedHandler(PythonScriptHostService pythonScriptHostService, ILogger<MqttClientDisconnectedHandler> logger)
        {
            _pythonScriptHostService = pythonScriptHostService ?? throw new ArgumentNullException(nameof(pythonScriptHostService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task HandleClientDisconnectedAsync(MqttServerClientDisconnectedEventArgs eventArgs)
        {
            try
            {
                var pythonEventArgs = new PythonDictionary
                {
                    { "client_id", eventArgs.ClientId },
                    { "type", PythonConvert.Pythonfy(eventArgs.DisconnectType) }
                };

                _pythonScriptHostService.InvokeOptionalFunction("on_client_disconnected", pythonEventArgs);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while handling client disconnected event.");
            }

            return Task.CompletedTask;
        }
    }
}
