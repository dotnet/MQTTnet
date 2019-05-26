using System;
using System.Text;
using System.Threading.Tasks;
using IronPython.Runtime;
using Microsoft.Extensions.Logging;
using MQTTnet.Protocol;
using MQTTnet.Server.Scripting;

namespace MQTTnet.Server.Mqtt
{
    public class MqttConnectionValidator : IMqttServerConnectionValidator
    {
        private readonly PythonScriptHostService _pythonScriptHostService;
        private readonly ILogger _logger;

        public MqttConnectionValidator(PythonScriptHostService pythonScriptHostService, ILogger<MqttConnectionValidator> logger)
        {
            _pythonScriptHostService = pythonScriptHostService ?? throw new ArgumentNullException(nameof(pythonScriptHostService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task ValidateConnectionAsync(MqttConnectionValidatorContext context)
        {
            try
            {
                string passwordString = null;
                if (context.Password != null)
                {
                    passwordString = Encoding.UTF8.GetString(context.Password);
                }

                var pythonContext = new PythonDictionary
                {
                    { "client_id", context.ClientId },
                    { "endpoint", context.Endpoint },
                    { "is_secure_connection", context.IsSecureConnection },
                    { "username", context.Username },
                    { "password", context.Password },
                    { "password_string", passwordString },
                    { "result", PythonConvert.Pythonfy(context.ReturnCode) }
                };

                _pythonScriptHostService.InvokeOptionalFunction("on_validate_client_connection", pythonContext);

                context.ReturnCode = PythonConvert.ParseEnum<MqttConnectReturnCode>((string)pythonContext["result"]);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while validating client connection.");
            }

            return Task.CompletedTask;
        }
    }
}
