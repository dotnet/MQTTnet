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
    public class ClientsController : Controller
    {
        private readonly MqttServerService _mqttServerService;

        public ClientsController(MqttServerService mqttServerService)
        {
            _mqttServerService = mqttServerService ?? throw new ArgumentNullException(nameof(mqttServerService));
        }

        [Route("api/v1/clients")]
        [HttpGet]
        public async Task<ActionResult<IList<IMqttClientStatus>>> GetClients()
        {
            return new ObjectResult(await _mqttServerService.GetClientStatusAsync());
        }

        [Route("api/v1/clients/{clientId}")]
        [HttpGet]
        public async Task<ActionResult<IMqttClientStatus>> GetClient(string clientId)
        {
            clientId = HttpUtility.UrlDecode(clientId);

            var client = (await _mqttServerService.GetClientStatusAsync()).FirstOrDefault(c => c.ClientId == clientId);
            if (client == null)
            {
                return new StatusCodeResult((int)HttpStatusCode.NotFound);
            }

            return new ObjectResult(client);
        }

        [Route("api/v1/clients/{clientId}")]
        [HttpDelete]
        public async Task<ActionResult> DeleteClient(string clientId)
        {
            clientId = HttpUtility.UrlDecode(clientId);

            var client = (await _mqttServerService.GetClientStatusAsync()).FirstOrDefault(c => c.ClientId == clientId);
            if (client == null)
            {
                return new StatusCodeResult((int)HttpStatusCode.NotFound);
            }

            await client.DisconnectAsync();
            return StatusCode((int)HttpStatusCode.NoContent);
        }

        [Route("api/v1/clients/{clientId}/statistics")]
        [HttpDelete]
        public async Task<ActionResult> DeleteClientStatistics(string clientId)
        {
            clientId = HttpUtility.UrlDecode(clientId);

            var client = (await _mqttServerService.GetClientStatusAsync()).FirstOrDefault(c => c.ClientId == clientId);
            if (client == null)
            {
                return new StatusCodeResult((int)HttpStatusCode.NotFound);
            }

            client.ResetStatistics();
            return StatusCode((int)HttpStatusCode.NoContent);
        }
    }
}
