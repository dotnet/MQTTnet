using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MQTTnet.Protocol;
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
        public async Task<ActionResult> PostMessage(string topic, int qosLevel = 0)
        {
            byte[] payload;

            using (var memoryStream = new MemoryStream())
            {
                await HttpContext.Request.Body.CopyToAsync(memoryStream);
                payload = memoryStream.ToArray();
            }

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel((MqttQualityOfServiceLevel)qosLevel)
                .Build();

            return await PostMessage(message);
        }
    }
}
