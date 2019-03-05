using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IronPython.Runtime;
using Microsoft.Extensions.Logging;
using MQTTnet.Adapter;
using MQTTnet.AspNetCore;
using MQTTnet.Implementations;
using MQTTnet.Protocol;
using MQTTnet.Server.Logging;
using MQTTnet.Server.Scripting;

namespace MQTTnet.Server.Mqtt
{
    public class MqttServerService
    {
        private readonly ILogger<MqttServerService> _logger;

        private readonly MqttConnectionValidator _mqttConnectionValidator;
        private readonly MqttSubscriptionInterceptor _mqttSubscriptionInterceptor;
        private readonly MqttApplicationMessageInterceptor _mqttApplicationMessageInterceptor;
        private readonly PythonScriptHostService _pythonScriptHostService;

        private readonly IMqttServer _mqttServer;

        public MqttServerService(
            IMqttServerFactory mqttServerFactory,
            MqttWebSocketServerAdapter webSocketServerAdapter,
            MqttNetLoggerWrapper mqttNetLogger,
            MqttConnectionValidator mqttConnectionValidator,
            MqttSubscriptionInterceptor mqttSubscriptionInterceptor,
            MqttApplicationMessageInterceptor mqttApplicationMessageInterceptor,
            PythonScriptHostService pythonScriptHostService,
            ILogger<MqttServerService> logger)
        {
            _mqttConnectionValidator = mqttConnectionValidator ?? throw new ArgumentNullException(nameof(mqttConnectionValidator));
            _mqttSubscriptionInterceptor = mqttSubscriptionInterceptor ?? throw new ArgumentNullException(nameof(mqttSubscriptionInterceptor));
            _mqttApplicationMessageInterceptor = mqttApplicationMessageInterceptor ?? throw new ArgumentNullException(nameof(mqttApplicationMessageInterceptor));
            _pythonScriptHostService = pythonScriptHostService ?? throw new ArgumentNullException(nameof(pythonScriptHostService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var adapters = new List<IMqttServerAdapter>
            {
                new MqttTcpServerAdapter(new MqttNetChildLoggerWrapper(null, mqttNetLogger)),
                webSocketServerAdapter
            };

            _mqttServer = mqttServerFactory.CreateMqttServer(adapters);
        }

        public void Configure()
        {
            _pythonScriptHostService.RegisterProxyObject("publish", new Action<PythonDictionary>(Publish));

            var options = new MqttServerOptionsBuilder()
                .WithDefaultEndpoint()
                .WithDefaultEndpointPort(1883)
                .WithConnectionValidator(_mqttConnectionValidator)
                .WithApplicationMessageInterceptor(_mqttApplicationMessageInterceptor)
                .WithSubscriptionInterceptor(_mqttSubscriptionInterceptor)
                .Build();

            _mqttServer.StartAsync(options).GetAwaiter().GetResult();

            _logger.LogInformation("MQTT server started.");
        }

        private void Publish(PythonDictionary parameters)
        {
            var applicationMessageBuilder = new MqttApplicationMessageBuilder()
                .WithTopic((string)parameters.get("topic", null))
                .WithRetainFlag((bool)parameters.get("retain", false))
                .WithQualityOfServiceLevel((MqttQualityOfServiceLevel)(int)parameters.get("qos", 0));

            var payload = parameters.get("payload", null);
            var binaryPayload = new byte[0];

            if (payload is string stringPayload)
            {
                binaryPayload = Encoding.UTF8.GetBytes(stringPayload);
            }
            else if (payload is ByteArray byteArray)
            {
                binaryPayload = byteArray.ToArray();
            }
            else if (payload is IEnumerable<int> intArray)
            {
                binaryPayload = intArray.Select(Convert.ToByte).ToArray();
            }

            applicationMessageBuilder = applicationMessageBuilder
                .WithPayload(binaryPayload);

            var applicationMessage = applicationMessageBuilder.Build();

            _mqttServer.PublishAsync(applicationMessage).GetAwaiter().GetResult();
            _logger.LogInformation($"Published topic '{applicationMessage.Topic}' from server.");
        }
    }
}
