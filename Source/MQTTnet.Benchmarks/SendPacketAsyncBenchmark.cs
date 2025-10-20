using System.Buffers;
using System.IO.Pipelines;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using MQTTnet.Formatter;

namespace MQTTnet.Benchmarks;

[SimpleJob(RuntimeMoniker.Net60)]
[RPlotExporter]
[RankColumn]
[MemoryDiagnoser]
public sealed class SendPacketAsyncBenchmark : BaseBenchmark, IDisposable, IAsyncDisposable
{
    MqttPacketBuffer _buffer;
    MemoryStream _stream;

    [Benchmark]
    public async ValueTask After()
    {
        _stream.Position = 0;
        var output = PipeWriter.Create(_stream);

        if (_buffer.Payload.Length == 0)
        {
            await output.WriteAsync(_buffer.Packet).ConfigureAwait(false);
        }
        else
        {
            WritePacketBuffer(output, _buffer);
            await output.FlushAsync().ConfigureAwait(false);
        }
    }

    [Benchmark(Baseline = true)]
    public async ValueTask Before()
    {
        _stream.Position = 0;
        var output = PipeWriter.Create(_stream);

        WritePacketBuffer(output, _buffer);
        await output.FlushAsync();
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

    [GlobalSetup]
    public void GlobalSetup()
    {
        _stream = new MemoryStream(1024);
        var packet = new ArraySegment<byte>(new byte[10]);
        _buffer = new MqttPacketBuffer(packet);
    }

    static void WritePacketBuffer(PipeWriter output, MqttPacketBuffer buffer)
    {
        // copy MqttPacketBuffer's Packet and Payload to the same buffer block of PipeWriter
        // MqttPacket will be transmitted within the bounds of a WebSocket frame after PipeWriter.FlushAsync

        var span = output.GetSpan(buffer.Length);

        buffer.Packet.AsSpan().CopyTo(span);
        buffer.Payload.CopyTo(span[buffer.Packet.Count..]);

        output.Advance(buffer.Length);
    }
}