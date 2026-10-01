using System.Net.Sockets;
using MQTTnet.Tests.Mockups;

namespace MQTTnet.Tests.Server;

[TestClass]
public sealed class StrictUtf8Wire_Tests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Invalid_Connect_Client_Id_Is_Rejected_Before_Validation(bool nullCharacter)
    {
        using var environment = new TestEnvironment { IgnoreServerLogErrors = true };
        var server = await environment.StartServer();
        var validations = 0;
        server.ValidatingConnectionAsync += _ => { Interlocked.Increment(ref validations); return Task.CompletedTask; };
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", environment.ServerPort, timeout.Token);
        var stream = client.GetStream();
        var id = nullCharacter ? new byte[] { 0 } : new byte[] { 0xC0, 0x80 };
        byte[] body = [0, 4, (byte)'M', (byte)'Q', (byte)'T', (byte)'T', 5, 2, 0, 30, 0, 0, (byte)id.Length, .. id];
        byte[] connectPacket = [0x10, (byte)body.Length, .. body];
        await stream.WriteAsync(connectPacket, timeout.Token);
        var reply = await ReadPacketOrClose(stream, timeout.Token);
        Assert.AreEqual(0, Volatile.Read(ref validations));
        if (reply.Length > 0)
        {
            Assert.AreEqual((byte)0x20, reply[0]);
            Assert.AreEqual((byte)0x81, reply[3]);
            Assert.IsEmpty(await ReadPacketOrClose(stream, timeout.Token));
        }
        Assert.AreEqual(0, Volatile.Read(ref validations));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Invalid_User_Property_Value_Is_Rejected_Before_Business_Callback(bool nullCharacter)
    {
        using var environment = new TestEnvironment { IgnoreServerLogErrors = true };
        var server = await environment.StartServer();
        var publications = 0;
        server.InterceptingPublishAsync += _ => { Interlocked.Increment(ref publications); return Task.CompletedTask; };
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", environment.ServerPort, timeout.Token);
        var stream = client.GetStream();
        byte[] connect = [0x10, 14, 0, 4, (byte)'M', (byte)'Q', (byte)'T', (byte)'T', 5, 2, 0, 30, 0, 0, 1, (byte)'a'];
        await stream.WriteAsync(connect, timeout.Token);
        var connAck = await ReadPacketOrClose(stream, timeout.Token);
        Assert.AreEqual((byte)0x20, connAck[0]);
        Assert.AreEqual((byte)0, connAck[3]);
        var value = nullCharacter ? new byte[] { 0 } : new byte[] { 0xC0, 0x80 };
        byte[] property = [0x26, 0, 1, (byte)'n', 0, (byte)value.Length, .. value];
        byte[] body = [0, 1, (byte)'a', 0, 1, (byte)property.Length, .. property];
        byte[] publishPacket = [0x32, (byte)body.Length, .. body];
        await stream.WriteAsync(publishPacket, timeout.Token);
        var reply = await ReadPacketOrClose(stream, timeout.Token);
        Assert.AreEqual(0, Volatile.Read(ref publications));
        if (reply.Length > 0)
        {
            Assert.AreEqual((byte)0xE0, reply[0]);
            Assert.AreEqual((byte)0x81, reply[2]);
            Assert.IsEmpty(await ReadPacketOrClose(stream, timeout.Token));
        }
        Assert.AreEqual(0, Volatile.Read(ref publications));
    }

    static async Task<byte[]> ReadPacketOrClose(NetworkStream stream, CancellationToken token)
    {
        try
        {
            var header = new byte[1];
            if (await stream.ReadAsync(header, token) == 0) return [];
            var bytes = new List<byte> { header[0] };
            var remaining = 0;
            var multiplier = 1;
            byte digit;
            do
            {
                await stream.ReadExactlyAsync(header, token);
                digit = header[0]; bytes.Add(digit);
                remaining += (digit & 127) * multiplier; multiplier *= 128;
                if (bytes.Count > 5) throw new InvalidDataException("Invalid packet length.");
            } while ((digit & 128) != 0);
            var body = new byte[remaining];
            await stream.ReadExactlyAsync(body, token);
            bytes.AddRange(body);
            return bytes.ToArray();
        }
        catch (IOException exception) when (exception.InnerException is SocketException { SocketErrorCode: SocketError.ConnectionReset }) { return []; }
    }
}
