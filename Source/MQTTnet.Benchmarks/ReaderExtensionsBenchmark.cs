using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using MQTTnet.AspNetCore;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace MQTTnet.Benchmarks;

[SimpleJob(RuntimeMoniker.Net60)]
[RPlotExporter, RankColumn]
[MemoryDiagnoser]
public sealed class ReaderExtensionsBenchmark : IDisposable, IAsyncDisposable
{
    MqttPacketFormatterAdapter _mqttPacketFormatter;
    MemoryStream _stream;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _mqttPacketFormatter = new MqttPacketFormatterAdapter(MqttProtocolVersion.V311, new MqttBufferWriter(4096, 65535));
        var mqttMessage = new MqttApplicationMessageBuilder()
            .WithTopic("topic")
            .WithPayload(new byte[10 * 1024])
            .Build();

        var packet = MqttPublishPacketFactory.Create(mqttMessage);

        var buffer = _mqttPacketFormatter.Encode(packet);
        _stream = new MemoryStream();
        _stream.Write(buffer.Packet);
        _stream.Write(buffer.Payload.ToArray());
        _mqttPacketFormatter.Cleanup();
    }

    [Benchmark]
    public async Task After()
    {
        _stream.Position = 0;
        var input = PipeReader.Create(_stream);

        while (true)
        {
            ReadResult readResult;
            var readTask = input.ReadAsync();
            if (readTask.IsCompleted)
            {
                readResult = readTask.Result;
            }
            else
            {
                readResult = await readTask.ConfigureAwait(false);
            }

            var buffer = readResult.Buffer;

            var consumed = buffer.Start;
            var observed = buffer.Start;

            try
            {
                if (!buffer.IsEmpty)
                {
                    if (_mqttPacketFormatter.TryDecode(buffer, out _, out consumed, out observed, out _))
                    {
                        break;
                    }
                }
                else if (readResult.IsCompleted)
                {
                    throw new MqttCommunicationException("Connection Aborted");
                }
            }
            finally
            {
                // The buffer was sliced up to where it was consumed, so we can just advance to the start.
                // We mark examined as buffer.End so that if we didn't receive a full frame, we'll wait for more data
                // before yielding the read again.
                input.AdvanceTo(consumed, observed);
            }
        }
    }

    public void Dispose()
    {
        _stream?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_stream != null)
        {
            await _stream.DisposeAsync();
        }
    }
}