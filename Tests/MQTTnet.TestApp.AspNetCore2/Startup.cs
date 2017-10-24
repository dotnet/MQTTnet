using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using MQTTnet.AspnetCore;
using MQTTnet.Core;

namespace MQTTnet.TestApp.AspNetCore2
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHostedMqttServer();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseMqttEndpoint();
            app.UseMqttServer(async server =>
            {
                var msg = new MqttApplicationMessageBuilder()
                    .WithPayload("Mqtt is awesome")
                    .WithTopic("message");

                while (true)
                {
                    server.Publish(msg.Build());
                    await Task.Delay(TimeSpan.FromSeconds(2));
                    msg.WithPayload("Mqtt is still awesome at " + DateTime.Now);
                }
            });

            app.Use((context, next) =>
            {
                if (context.Request.Path == "/")
                {
                    context.Request.Path = "/Index.html";
                }

                return next();
            });

            app.UseStaticFiles();


            app.UseStaticFiles( new StaticFileOptions()
            {
                RequestPath = "/node_modules",
                FileProvider = new PhysicalFileProvider( Path.Combine(env.ContentRootPath, "node_modules" ) )
            } );
        }
    }
}
