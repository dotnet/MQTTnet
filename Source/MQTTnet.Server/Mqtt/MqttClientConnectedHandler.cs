using System;
using System.Threading.Tasks;
using IronPython.Runtime;
using Microsoft.Extensions.Logging;
using MQTTnet.Server.Scripting;

namespace MQTTnet.Server.Mqtt
{
    public class MqttClientConnectedHandler : IMqttServerClientConnectedHandler
    {
        private readonly PythonScriptHostService _pythonScriptHostService;
        private readonly ILogger _logger;

        public MqttClientConnectedHandler(PythonScriptHostService pythonScriptHostService, ILogger<MqttClientConnectedHandler> logger)
        {
            _pythonScriptHostService = pythonScriptHostService ?? throw new ArgumentNullException(nameof(pythonScriptHostService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task HandleClientConnectedAsync(MqttServerClientConnectedEventArgs eventArgs)
        {
            try
            {
                var pythonEventArgs = new PythonDictionary
                {
                    { "client_id", eventArgs.ClientId }
                };

                _pythonScriptHostService.InvokeOptionalFunction("on_client_connected", pythonEventArgs);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while handling client connected event.");
            }

            return Task.CompletedTask;
        }
    }
}
