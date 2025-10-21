// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Exceptions;

namespace MQTTnet.Tests.Exceptions;

[TestClass]
public class MqttProtocolViolationException_Tests
{
    [TestMethod]
    public void No_For_Max_Large_Variable_Byte_Integer()
    {
        MqttProtocolViolationException.ThrowIfVariableByteIntegerExceedsLimit(MqttProtocolViolationException.VariableByteIntegerMaxValue);
    }

    [TestMethod]
    public void Throw_Exception_For_Too_Large_Variable_Byte_Integer()
    {
        Assert.ThrowsExactly<MqttProtocolViolationException>(() => MqttProtocolViolationException.ThrowIfVariableByteIntegerExceedsLimit(1726790899));
    }
}