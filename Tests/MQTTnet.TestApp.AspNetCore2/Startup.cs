using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using MQTTnet.AspNetCore;
using MQTTnet.Server;

namespace MQTTnet.TestApp.AspNetCore2
{
    public class Startup
    {
        // In class _Startup_ of the ASP.NET Core 2.0 project.

        public void ConfigureServices(IServiceCollection services)
        {
            var mqttServerOptions = new MqttServerOptionsBuilder().Build();
            services.AddHostedMqttServer(mqttServerOptions);
        }

        // In class _Startup_ of the ASP.NET Core 2.0 project.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseMqttEndpoint();
            app.UseMqttServer(server =>
            {
                server.Started += async (sender, args) =>
                {
                    var msg = new MqttApplicationMessageBuilder()
                        .WithPayload("Mqtt is awesome")
                        .WithTopic("message");

                    while (true)
                    {
                        try
                        {
                            await server.PublishAsync(msg.Build());
                            msg.WithPayload("Mqtt is still awesome at " + DateTime.Now);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                        finally
                        {
                            await Task.Delay(TimeSpan.FromSeconds(2));
                        }
                    }
                };
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


            app.UseStaticFiles(new StaticFileOptions
            {
                RequestPath = "/node_modules",
                FileProvider = new PhysicalFileProvider(Path.Combine(env.ContentRootPath, "node_modules"))
            });
        }
    }
}
