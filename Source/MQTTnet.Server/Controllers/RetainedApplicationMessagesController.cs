using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using MQTTnet.Server.Mqtt;

namespace MQTTnet.Server.Controllers
{
    [ApiController]
    public class RetainedApplicationMessagesController : ControllerBase
    {
        private readonly MqttServerService _mqttServerService;

        public RetainedApplicationMessagesController(MqttServerService mqttServerService)
        {
            _mqttServerService = mqttServerService ?? throw new ArgumentNullException(nameof(mqttServerService));
        }

        [Route("api/v1/retainedApplicationMessages")]
        [HttpGet]
        public async Task<ActionResult<IList<MqttApplicationMessage>>> GetRetainedApplicationMessages()
        {
            return new ObjectResult(await _mqttServerService.GetRetainedApplicationMessagesAsync());
        }

        [Route("api/v1/retainedApplicationMessages/{topic}")]
        [HttpGet]
        public async Task<ActionResult<MqttApplicationMessage>> GetRetainedApplicationMessage(string topic)
        {
            topic = HttpUtility.UrlDecode(topic);

            var applicationMessage = (await _mqttServerService.GetRetainedApplicationMessagesAsync()).FirstOrDefault(c => c.Topic == topic);
            if (applicationMessage == null)
            {
                return new StatusCodeResult((int)HttpStatusCode.NotFound);
            }

            return new ObjectResult(applicationMessage);
        }

        [Route("api/v1/retainedApplicationMessages")]
        [HttpDelete]
        public async Task<ActionResult> DeleteRetainedApplicationMessages()
        {
            await _mqttServerService.ClearRetainedApplicationMessagesAsync();
            return StatusCode((int)HttpStatusCode.NoContent);
        }

        [Route("api/v1/retainedApplicationMessages/{topic}")]
        [HttpDelete]
        public async Task<ActionResult> DeleteRetainedApplicationMessage(string topic)
        {
            topic = HttpUtility.UrlDecode(topic);

            await _mqttServerService.PublishAsync(new MqttApplicationMessageBuilder().WithTopic(topic).WithRetainFlag().Build());
            return StatusCode((int)HttpStatusCode.NoContent);
        }
    }
}
