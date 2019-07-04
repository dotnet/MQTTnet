using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MQTTnet.Server.Mqtt;

namespace MQTTnet.Server.Controllers
{
    [Authorize]
    [ApiController]
    public class MessagesController : Controller
    {
        private readonly MqttServerService _mqttServerService;

        public MessagesController(MqttServerService mqttServerService)
        {
            _mqttServerService = mqttServerService ?? throw new ArgumentNullException(nameof(mqttServerService));
        }

        [Route("api/v1/messages")]
        [HttpPost]
        public async Task<ActionResult> PostMessage(MqttApplicationMessage message)
        {
            await _mqttServerService.PublishAsync(message);
            return Ok();
        }

        [Route("api/v1/messages/{*topic}")]
        [HttpPost]
        public Task<ActionResult> PostMessage(string topic, string payload)
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .Build();

            return PostMessage(message);
        }
    }
}
