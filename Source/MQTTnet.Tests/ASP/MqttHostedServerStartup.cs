using Microsoft.AspNetCore.Builder;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.AspNetCore;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Server;
using Microsoft.AspNetCore.Hosting;

namespace MQTTnet.Tests.ASP
{
    [TestClass]
    public class MqttHostedServerStartup
    {
        private async Task TestStartup(bool useOccupiedPort)
        {
            using TcpListener l = new TcpListener(IPAddress.Any, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;

            if(!useOccupiedPort)
                l.Stop();


            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseUrls("http://127.0.0.1:0");

            builder.Services.AddMqttTcpServerAdapter();
            builder.Services.AddHostedMqttServer(cfg =>
            {
                cfg
                    .WithDefaultEndpoint()
                    .WithDefaultEndpointPort(port);
            });


            var app = builder.Build();

            if(!useOccupiedPort)
            {
                await app.StartAsync();
                var server = app.Services.GetRequiredService<MqttServer>();
                Assert.IsTrue(server.IsStarted);
                await app.StopAsync();
            }
            else
            {
                await Assert.ThrowsExceptionAsync<SocketException>(() =>
                    app.StartAsync()
                    );
            }
        }

        [TestMethod]
        [DoNotParallelize]
        public Task TestSuccessfullyStartup()
            => TestStartup(useOccupiedPort: false);

        [TestMethod]
        [DoNotParallelize]
        public Task TestFailedStartup()
            => TestStartup(useOccupiedPort: true);

    }
}
