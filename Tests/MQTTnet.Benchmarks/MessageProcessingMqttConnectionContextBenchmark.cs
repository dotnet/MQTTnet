using BenchmarkDotNet.Attributes;
using MQTTnet.Client;


using MQTTnet.AspNetCore;

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using MQTTnet.Server;
using MQTTnet.Diagnostics;
using MQTTnet.AspNetCore.Client;
using MQTTnet.AspNetCore.Extensions;
using MQTTnet.Client.Options;

namespace MQTTnet.Benchmarks
{
    [MemoryDiagnoser]
    public class MessageProcessingMqttConnectionContextBenchmark
    {
        private IWebHost _host;
        private IMqttClient _mqttClient;
        private MqttApplicationMessage _message;

        [GlobalSetup]
        public void Setup()
        {
            _host = WebHost.CreateDefaultBuilder()
                   .UseKestrel(o => o.ListenAnyIP(1883, l => l.UseMqtt()))
                   .ConfigureServices(services => {
                        services
                            .AddHostedMqttServer(mqttServerOptions => mqttServerOptions.WithoutDefaultEndpoint())
                            .AddMqttConnectionHandler();
                   })
                   .Configure(app => {
                       app.UseMqttServer(s => {

                       });
                   })
                   .Build();

            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient(new MqttNetEventLogger(), new MqttClientConnectionContextFactory());

            _host.StartAsync().GetAwaiter().GetResult();

            var clientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer("localhost").Build();

            _mqttClient.ConnectAsync(clientOptions).GetAwaiter().GetResult();

            _message = new MqttApplicationMessageBuilder()
                .WithTopic("A")
                .Build();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _mqttClient.DisconnectAsync().GetAwaiter().GetResult();
            _mqttClient.Dispose();

            _host.StopAsync().GetAwaiter().GetResult();
            _host.Dispose();
        }

        [Benchmark]
        public void Send_10000_Messages()
        {
            for (var i = 0; i < 10000; i++)
            {
                _mqttClient.PublishAsync(_message).GetAwaiter().GetResult();
            }
        }
    }
}
