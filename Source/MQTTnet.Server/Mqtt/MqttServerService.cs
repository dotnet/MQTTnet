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
using MQTTnet.Server.Scripting;

namespace MQTTnet.Server.Mqtt
{
    public class MqttServerService
    {
        private readonly ILogger<MqttServerService> _logger;

        private readonly MqttClientConnectedHandler _mqttClientConnectedHandler;
        private readonly MqttClientDisconnectedHandler _mqttClientDisconnectedHandler;
        private readonly MqttClientSubscribedTopicHandler _mqttClientSubscribedTopicHandler;
        private readonly MqttClientUnsubscribedTopicHandler _mqttClientUnsubscribedTopicHandler;
        private readonly MqttConnectionValidator _mqttConnectionValidator;
        private readonly MqttSubscriptionInterceptor _mqttSubscriptionInterceptor;
        private readonly MqttApplicationMessageInterceptor _mqttApplicationMessageInterceptor;
        private readonly PythonScriptHostService _pythonScriptHostService;

        private readonly IMqttServer _mqttServer;

        public MqttServerService(
            CustomMqttFactory mqttFactory,
            MqttWebSocketServerAdapter webSocketServerAdapter,
            MqttClientConnectedHandler mqttClientConnectedHandler,
            MqttClientDisconnectedHandler mqttClientDisconnectedHandler,
            MqttClientSubscribedTopicHandler mqttClientSubscribedTopicHandler,
            MqttClientUnsubscribedTopicHandler mqttClientUnsubscribedTopicHandler,
            MqttConnectionValidator mqttConnectionValidator,
            MqttSubscriptionInterceptor mqttSubscriptionInterceptor,
            MqttApplicationMessageInterceptor mqttApplicationMessageInterceptor,
            PythonScriptHostService pythonScriptHostService,
            ILogger<MqttServerService> logger)
        {
            _mqttClientConnectedHandler = mqttClientConnectedHandler ?? throw new ArgumentNullException(nameof(mqttClientConnectedHandler));
            _mqttClientDisconnectedHandler = mqttClientDisconnectedHandler ?? throw new ArgumentNullException(nameof(mqttClientDisconnectedHandler));
            _mqttClientSubscribedTopicHandler = mqttClientSubscribedTopicHandler ?? throw new ArgumentNullException(nameof(mqttClientSubscribedTopicHandler));
            _mqttClientUnsubscribedTopicHandler = mqttClientUnsubscribedTopicHandler ?? throw new ArgumentNullException(nameof(mqttClientUnsubscribedTopicHandler));
            _mqttConnectionValidator = mqttConnectionValidator ?? throw new ArgumentNullException(nameof(mqttConnectionValidator));
            _mqttSubscriptionInterceptor = mqttSubscriptionInterceptor ?? throw new ArgumentNullException(nameof(mqttSubscriptionInterceptor));
            _mqttApplicationMessageInterceptor = mqttApplicationMessageInterceptor ?? throw new ArgumentNullException(nameof(mqttApplicationMessageInterceptor));
            _pythonScriptHostService = pythonScriptHostService ?? throw new ArgumentNullException(nameof(pythonScriptHostService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var adapters = new List<IMqttServerAdapter>
            {
                new MqttTcpServerAdapter(mqttFactory.Logger.CreateChildLogger(nameof(MqttTcpServerAdapter))),
                webSocketServerAdapter
            };

            _mqttServer = mqttFactory.CreateMqttServer(adapters);
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

            _mqttServer.ClientConnectedHandler = _mqttClientConnectedHandler;
            _mqttServer.ClientDisconnectedHandler = _mqttClientDisconnectedHandler;
            _mqttServer.ClientSubscribedTopicHandler = _mqttClientSubscribedTopicHandler;
            _mqttServer.ClientUnsubscribedTopicHandler = _mqttClientUnsubscribedTopicHandler;

            _mqttServer.StartAsync(options).GetAwaiter().GetResult();

            _logger.LogInformation("MQTT server started.");
        }

        private void Publish(PythonDictionary parameters)
        {
            try
            {
                var applicationMessageBuilder = new MqttApplicationMessageBuilder()
                    .WithTopic((string)parameters.get("topic", null))
                    .WithRetainFlag((bool)parameters.get("retain", false))
                    .WithQualityOfServiceLevel((MqttQualityOfServiceLevel)(int)parameters.get("qos", 0));

                var payload = parameters.get("payload", null);
                byte[] binaryPayload;

                if (payload == null)
                {
                    binaryPayload = new byte[0];
                }
                else if (payload is string stringPayload)
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
                else
                {
                    throw new NotSupportedException("Payload type not supported.");
                }

                applicationMessageBuilder = applicationMessageBuilder
                    .WithPayload(binaryPayload);

                var applicationMessage = applicationMessageBuilder.Build();

                _mqttServer.PublishAsync(applicationMessage).GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while publishing application message from server.");
            }
        }
    }
}
