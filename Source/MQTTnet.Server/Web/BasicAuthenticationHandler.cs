using System;
using System.IO;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using IronPython.Runtime;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Microsoft.Scripting;

namespace MQTTnet.Server.Web
{
    public class AuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly ILogger<AuthenticationHandler> _logger;

        public AuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory loggerFactory,
            UrlEncoder encoder,
            ISystemClock clock,
            ILogger<AuthenticationHandler> logger)
            : base(options, loggerFactory, encoder, clock)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey(HeaderNames.Authorization))
            {
                return AuthenticateResult.NoResult();
            }

            try
            {
                var headerValue = Request.Headers[HeaderNames.Authorization];
                var parsedHeaderValue = AuthenticationHeaderValue.Parse(Request.Headers[HeaderNames.Authorization]);

                var scheme = parsedHeaderValue.Scheme;
                var parameter = parsedHeaderValue.Parameter;
                string username = null;
                string password = null;

                if (scheme.Equals("Basic", StringComparison.OrdinalIgnoreCase))
                {
                    var credentialBytes = Convert.FromBase64String(parsedHeaderValue.Parameter);
                    var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':');
                    username = credentials[0];
                    password = credentials[1];
                }

                var context = new PythonDictionary
                {
                    ["header_value"] = headerValue,
                    ["scheme"] = scheme,
                    ["parameter"] = parameter,
                    ["username"] = username,
                    ["password"] = password,
                    ["is_authenticated"] = false
                };

                if (!ValidateUser(context))
                {
                    return AuthenticateResult.Fail("Invalid credentials.");
                }

                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, context.get("username") as string ?? string.Empty)
                };

                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);

                return AuthenticateResult.Success(ticket);
            }
            catch (Exception exception)
            {
                _logger.LogWarning("Error while authenticating user.", exception);

                return AuthenticateResult.Fail(exception);
            }
            finally
            {
                await Task.CompletedTask;
            }
        }

        private bool ValidateUser(PythonDictionary context)
        {
            var handlerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Web", "authorization_handler.py");
            if (!File.Exists(handlerPath))
            {
                return false;
            }

            var code = File.ReadAllText(handlerPath);

            var scriptEngine = IronPython.Hosting.Python.CreateEngine();
            //scriptEngine.Runtime.IO.SetOutput(new PythonIOStream(_logger.), Encoding.UTF8);

            var scriptScope = scriptEngine.CreateScope();
            var scriptSource = scriptScope.Engine.CreateScriptSourceFromString(code, SourceCodeKind.File);
            var compiledCode = scriptSource.Compile();
            compiledCode.Execute(scriptScope);
            var function = scriptScope.Engine.Operations.GetMember<PythonFunction>(scriptScope, "handle_authenticate");
            scriptScope.Engine.Operations.Invoke(function, context);

            return context.get("is_authenticated", false) as bool? == true;
        }
    }
}
