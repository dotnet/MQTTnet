using BenchmarkDotNet.Attributes;
using MQTTnet.Channel;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Implementations;
using MQTTnet.Server;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client.Options;

namespace MQTTnet.Benchmarks
{
    [MemoryDiagnoser]
    public class MqttTcpChannelBenchmark
    {
        private IMqttServer _mqttServer;
        private IMqttChannel _serverChannel;
        private IMqttChannel _clientChannel;

        [GlobalSetup]
        public void Setup()
        {
            var factory = new MqttFactory();
            var tcpServer = new MqttTcpServerAdapter(new MqttNetLogger().CreateChildLogger());
            tcpServer.ClientAccepted += (sender, args) => _serverChannel = (IMqttChannel)args.Client.GetType().GetField("_channel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(args.Client);

            _mqttServer = factory.CreateMqttServer(new[] { tcpServer }, new MqttNetLogger());

            var serverOptions = new MqttServerOptionsBuilder().Build();
            _mqttServer.StartAsync(serverOptions).GetAwaiter().GetResult();

            var clientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer("localhost").Build();

            var tcpOptions = (MqttClientTcpOptions)clientOptions.ChannelOptions;
            _clientChannel = new MqttTcpChannel(new MqttClientOptions { ChannelOptions = tcpOptions });

            _clientChannel.ConnectAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        [Benchmark]
        public async Task Send_10000_Chunks()
        {
            var size = 5;
            var iterations = 10000;

            await Task.WhenAll(WriteAsync(iterations, size), ReadAsync(iterations, size));
        }

        private async Task ReadAsync(int iterations, int size)
        {
            await Task.Yield();

            var expected = iterations * size;
            long read = 0;

            while (read < expected)
            {
                var readresult = await _clientChannel.ReadAsync(new byte[size], 0, size, CancellationToken.None).ConfigureAwait(false);
                read += readresult;
            }
        }

        private async Task WriteAsync(int iterations, int size)
        {
            await Task.Yield();

            for (var i = 0; i < iterations; i++)
            {
                await _serverChannel.WriteAsync(new byte[size], 0, size, CancellationToken.None).ConfigureAwait(false);
            }
        }
    }
}
