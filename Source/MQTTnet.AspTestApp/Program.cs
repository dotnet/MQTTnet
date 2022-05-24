// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet;
using MQTTnet.AspNetCore;
using MQTTnet.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

// Setup MQTT stuff.
builder.Services.AddMqttServer();
builder.Services.AddConnections();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

// Setup MQTT stuff.
app.UseEndpoints(endpoints =>
{
    endpoints.MapMqtt("/mqtt");
});

app.UseMqttServer(server =>
{
    server.StartedAsync += args =>
    {
        _ = Task.Run(async () =>
        {
            var mqttApplicationMessage = new MqttApplicationMessageBuilder()
                .WithPayload($"Test application message from MQTTnet server.")
                .WithTopic("message")
                .Build();

            while (true)
            {
                try
                {
                    await server.InjectApplicationMessage(new InjectedMqttApplicationMessage(mqttApplicationMessage)
                    {
                        SenderClientId = "server"
                    });
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                finally
                {
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
            }
        });

        return Task.CompletedTask;
    };
});

app.Run();
