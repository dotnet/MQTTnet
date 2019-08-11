using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MQTTnet.Server.Mqtt;
using MQTTnet.Server.Status;

namespace MQTTnet.Server.Controllers
{
    [Authorize]
    [ApiController]
    public class SessionsController : Controller
    {
        private readonly MqttServerService _mqttServerService;

        public SessionsController(MqttServerService mqttServerService)
        {
            _mqttServerService = mqttServerService ?? throw new ArgumentNullException(nameof(mqttServerService));
        }

        [Route("api/v1/sessions")]
        [HttpGet]
        public async Task<ActionResult<IList<IMqttSessionStatus>>> GetSessions()
        {
            return new ObjectResult(await _mqttServerService.GetSessionStatusAsync());
        }

        [Route("api/v1/sessions/{clientId}")]
        [HttpGet]
        public async Task<ActionResult<IMqttClientStatus>> GetSession(string clientId)
        {
            clientId = HttpUtility.UrlDecode(clientId);

            var session = (await _mqttServerService.GetSessionStatusAsync()).FirstOrDefault(c => c.ClientId == clientId);
            if (session == null)
            {
                return new StatusCodeResult((int)HttpStatusCode.NotFound);
            }

            return new ObjectResult(session);
        }

        [Route("api/v1/sessions/{clientId}")]
        [HttpDelete]
        public async Task<ActionResult> DeleteSession(string clientId)
        {
            clientId = HttpUtility.UrlDecode(clientId);

            var session = (await _mqttServerService.GetSessionStatusAsync()).FirstOrDefault(c => c.ClientId == clientId);
            if (session == null)
            {
                return new StatusCodeResult((int)HttpStatusCode.NotFound);
            }

            await session.DeleteAsync();
            return StatusCode((int)HttpStatusCode.NoContent);
        }

        [Route("api/v1/sessions/{clientId}/pendingApplicationMessages")]
        [HttpDelete]
        public async Task<ActionResult> DeletePendingApplicationMessages(string clientId)
        {
            clientId = HttpUtility.UrlDecode(clientId);

            var session = (await _mqttServerService.GetSessionStatusAsync()).FirstOrDefault(c => c.ClientId == clientId);
            if (session == null)
            {
                return new StatusCodeResult((int)HttpStatusCode.NotFound);
            }

            await session.ClearPendingApplicationMessagesAsync();
            return StatusCode((int)HttpStatusCode.NoContent);
        }
    }
}
