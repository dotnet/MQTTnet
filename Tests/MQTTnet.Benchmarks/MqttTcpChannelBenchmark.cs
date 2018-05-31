using BenchmarkDotNet.Attributes;
using MQTTnet.Adapter;
using MQTTnet.Channel;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Implementations;
using MQTTnet.Server;
using System.Threading;

namespace MQTTnet.Benchmarks
{
    [MemoryDiagnoser]
    public class MqttTcpChannelBenchmark
    {
        private IMqttServer _mqttServer;
        private IMqttChannel _clientChannel;
        private IMqttChannel _serverChannel;

        [GlobalSetup]
        public void Setup()
        {
            var factory = new MqttFactory();
            var tcpServer = new MqttTcpServerAdapter(new MqttNetLogger().CreateChildLogger());
            tcpServer.ClientAccepted += (sender, args) => _serverChannel = (IMqttChannel)args.Client.GetType().GetField("_channel", System.Reflection.BindingFlags.NonPublic| System.Reflection.BindingFlags.Instance).GetValue(args.Client);

            _mqttServer = factory.CreateMqttServer(new[] { tcpServer }, new MqttNetLogger());

            var serverOptions = new MqttServerOptionsBuilder().Build();
            _mqttServer.StartAsync(serverOptions).GetAwaiter().GetResult();

            var clientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer("localhost").Build();

            _clientChannel = new MqttTcpChannel((MqttClientTcpOptions)clientOptions.ChannelOptions);

            _clientChannel.ConnectAsync(CancellationToken.None).GetAwaiter().GetResult();
        }
        
        [Benchmark]
        public void Send_10000_Chunks()
        {
            var size = 5;
            var iterations = 10000;
            for (var i = 0; i < iterations; i++)
            {
                _serverChannel.WriteAsync(new byte[size], 0, size, CancellationToken.None).GetAwaiter().GetResult();
                _clientChannel.ReadAsync(new byte[size], 0, size, CancellationToken.None).GetAwaiter().GetResult();
            }

        }
    }
}
