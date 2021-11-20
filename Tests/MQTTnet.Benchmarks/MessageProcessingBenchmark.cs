using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using MQTTnet.Client;
using MQTTnet.Server;
using IMqttClient = MQTTnet.Client.IMqttClient;

namespace MQTTnet.Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net461)]
    [RPlotExporter, RankColumn]
    [MemoryDiagnoser]
    public class MessageProcessingBenchmark
    {
        MqttServer _mqttServer;
        IMqttClient _mqttClient;
        MqttApplicationMessage _message;

        [GlobalSetup]
        public void Setup()
        {
            var serverOptions = new MqttServerOptionsBuilder().Build();
            
            var factory = new MqttFactory();
            _mqttServer = factory.CreateMqttServer(serverOptions);
            _mqttClient = factory.CreateMqttClient();

            _mqttServer.StartAsync().GetAwaiter().GetResult();

            var clientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer("localhost").Build();

            _mqttClient.ConnectAsync(clientOptions).GetAwaiter().GetResult();

            _message = new MqttApplicationMessageBuilder()
                .WithTopic("A")
                .Build();
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
