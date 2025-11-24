// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Packets;

namespace MQTTnet.Internal;

public sealed class MqttPacketBusItem
{
    AsyncTaskCompletionSource<MqttPacket> _promise;

    public MqttPacketBusItem(MqttPacket packet)
    {
        Packet = packet ?? throw new ArgumentNullException(nameof(packet));
    }

    public event EventHandler Completed;

    public MqttPacket Packet { get; }

    public void Cancel()
    {
        _promise?.TrySetCanceled();
    }

    public void Complete()
    {
        _promise?.TrySetResult(Packet);
        Completed?.Invoke(this, EventArgs.Empty);
    }

    public void Fail(Exception exception)
    {
        _promise?.TrySetException(exception);
    }

    public Task<MqttPacket> WaitAsync()
    {
        // Lazy initialization - only allocate when actually needed
        _promise ??= new AsyncTaskCompletionSource<MqttPacket>();
        return _promise.Task;
    }
}