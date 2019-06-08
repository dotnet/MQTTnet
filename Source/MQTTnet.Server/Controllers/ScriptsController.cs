using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MQTTnet.Server.Scripting;
using MQTTnet.Server.Web;

namespace MQTTnet.Server.Controllers
{
    [Authorize]
    [ApiController]
    public class ScriptsController : Controller
    {
        private readonly PythonScriptHostService _pythonScriptHostService;

        public ScriptsController(PythonScriptHostService pythonScriptHostService)
        {
            _pythonScriptHostService = pythonScriptHostService ?? throw new ArgumentNullException(nameof(pythonScriptHostService));
        }

        [Route("api/v1/scripts")]
        [HttpGet]
        public ActionResult<List<string>> GetScriptUids()
        {
            return _pythonScriptHostService.GetScriptUids();
        }

        [Route("api/v1/scripts/uid")]
        [HttpGet]
        public async Task<ActionResult<string>> GetScript(string uid)
        {
            var script = await _pythonScriptHostService.ReadScriptAsync(uid, HttpContext.RequestAborted);
            if (script == null)
            {
                return NotFound();
            }

            return Content(script);
        }

        [Route("api/v1/scripts/uid")]
        [HttpPost]
        public Task PostScript(string uid)
        {
            var code = HttpContext.Request.ReadBodyAsString();
            return _pythonScriptHostService.WriteScriptAsync(uid, code, CancellationToken.None);
        }

        [Route("api/v1/scripts/uid")]
        [HttpDelete]
        public Task DeleteScript(string uid)
        {
            return _pythonScriptHostService.DeleteScriptAsync(uid);
        }
        
        [Route("api/v1/scripts/initialize")]
        [HttpPost]
        public Task PostInitializeScripts()
        {
            return _pythonScriptHostService.TryInitializeScriptsAsync();
        }
    }
}
