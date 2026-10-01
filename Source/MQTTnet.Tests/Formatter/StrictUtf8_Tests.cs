using System.Buffers;
using System.Text;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using MQTTnet.Packets;

namespace MQTTnet.Tests.Formatter;

[TestClass]
public sealed class StrictUtf8_Tests
{
    [TestMethod]
    [DataRow("C080")]
    [DataRow("80")]
    [DataRow("EDA080")]
    [DataRow("F4908080")]
    [DataRow("FF")]
    [DataRow("E282")]
    [DataRow("00")]
    public void Generic_String_Rejects_Invalid_Encoding_And_Null(string hex)
    {
        var data = StringBytes(Convert.FromHexString(hex));
        var reader = new MqttBufferReader();
        reader.SetBuffer(data, 0, data.Length);
        Assert.ThrowsExactly<MqttProtocolViolationException>(() => reader.ReadString());
    }

    [TestMethod]
    [DataRow("C080")]
    [DataRow("80")]
    [DataRow("EDA080")]
    [DataRow("F4908080")]
    [DataRow("FF")]
    [DataRow("E282")]
    [DataRow("00")]
    public void Raw_User_Property_Value_Rejects_Invalid_Encoding_And_Null(string hex)
    {
        byte[] property = [0x26, 0, 1, (byte)'n', .. StringBytes(Convert.FromHexString(hex))];
        byte[] body = [0, 1, (byte)'a', (byte)property.Length, .. property];
        byte[] packet = [0x30, (byte)body.Length, .. body];
        Assert.ThrowsExactly<MqttProtocolViolationException>(() => MqttPacketSerializationHelper.DecodePacket(packet, MqttProtocolVersion.V500));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("EFBBBF61")]
    [DataRow("EFBFBD")]
    [DataRow("F09F9880")]
    [DataRow("01")]
    [DataRow("EFBFBF")]
    public void Valid_Strings_And_Ordered_Property_Bytes_Are_Preserved(string hex)
    {
        var bytes = Convert.FromHexString(hex);
        var expected = Encoding.UTF8.GetString(bytes);
        var data = StringBytes(bytes);
        var reader = new MqttBufferReader();
        reader.SetBuffer(data, 0, data.Length);
        Assert.AreEqual(expected, reader.ReadString());
        byte[] property = [0x26, 0, 1, (byte)'n', .. data];
        byte[] body = [0, 1, (byte)'a', (byte)(2 * property.Length), .. property, .. property];
        byte[] packet = [0x30, (byte)body.Length, .. body];
        var decoded = (MqttPublishPacket)MqttPacketSerializationHelper.DecodePacket(packet, MqttProtocolVersion.V500);
        Assert.HasCount(2, decoded.UserProperties);
        foreach (var item in decoded.UserProperties)
        {
            Assert.AreEqual("n", item.Name);
            CollectionAssert.AreEqual(bytes, item.ValueBuffer.ToArray());
        }
    }

    [TestMethod]
    public void Maximum_String_Length_And_Truncated_Bounds()
    {
        var data = StringBytes(Encoding.UTF8.GetBytes(new string('a', ushort.MaxValue)));
        var reader = new MqttBufferReader();
        reader.SetBuffer(data, 0, data.Length);
        Assert.AreEqual((int)ushort.MaxValue, reader.ReadString().Length);
        reader.SetBuffer(data, 0, data.Length - 1);
        Assert.ThrowsExactly<MqttProtocolViolationException>(() => reader.ReadString());
    }

    [TestMethod]
    public void Binary_Fields_Remain_Opaque()
    {
        byte[] bytes = [0xC0, 0x80, 0, 0xFF];
        var data = StringBytes(bytes);
        var reader = new MqttBufferReader();
        reader.SetBuffer(data, 0, data.Length);
        CollectionAssert.AreEqual(bytes, reader.ReadBinaryData());
        var auth = new MqttAuthPacket { ReasonCode = Protocol.MqttAuthenticateReasonCode.ContinueAuthentication,
            AuthenticationMethod = "method", AuthenticationData = bytes };
        var decoded = (MqttAuthPacket)MqttPacketSerializationHelper.DecodePacket(MqttPacketSerializationHelper.EncodePacket(auth, MqttProtocolVersion.V500), MqttProtocolVersion.V500);
        CollectionAssert.AreEqual(bytes, decoded.AuthenticationData);
        var publish = new MqttPublishPacket { Topic = "a", Payload = new System.Buffers.ReadOnlySequence<byte>(bytes), CorrelationData = bytes };
        var received = (MqttPublishPacket)MqttPacketSerializationHelper.DecodePacket(MqttPacketSerializationHelper.EncodePacket(publish, MqttProtocolVersion.V500), MqttProtocolVersion.V500);
        CollectionAssert.AreEqual(bytes, received.CorrelationData);
        CollectionAssert.AreEqual(bytes, received.Payload.ToArray());
    }

    internal static byte[] StringBytes(byte[] bytes) => [(byte)(bytes.Length >> 8), (byte)bytes.Length, .. bytes];
}
