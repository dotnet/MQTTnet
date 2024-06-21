// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Packets;

namespace MQTTnet.Client;

public sealed class MqttApplicationMessageReceivedEventArgs : EventArgs
{
    readonly Func<MqttApplicationMessageReceivedEventArgs, CancellationToken, Task> _acknowledgeHandler;

    int _isAcknowledged;

    public MqttApplicationMessageReceivedEventArgs(
        string clientId,
        MqttApplicationMessage applicationMessage,
        MqttPublishPacket publishPacket,
        Func<MqttApplicationMessageReceivedEventArgs, CancellationToken, Task> acknowledgeHandler)
    {
        ClientId = clientId;
        ApplicationMessage = applicationMessage ?? throw new ArgumentNullException(nameof(applicationMessage));
        PublishPacket = publishPacket ?? throw new ArgumentNullException(nameof(publishPacket));
        _acknowledgeHandler = acknowledgeHandler;
    }

    /// <summary>
    ///     The invoked message receiver can take ownership of the payload to avoid cloning.
    ///     If not cloned, it is the obligation of the new owner to dispose the payload by
    ///     calling <see cref="MqttApplicationMessage.DisposePayload()"/>.
    /// </summary>
    /// <param name="clonePayload">
    ///     If set to true, clones the applicationMessage and copies the payload.
    ///     The new instance does not need to be disposed.
    /// </param>
    public void TransferPayload(bool clonePayload)
    {
        TransferredPayload = true;
        if (clonePayload)
        {
            var applicationMessage = ApplicationMessage;
            if (applicationMessage != null)
            {
                ApplicationMessage = applicationMessage.Clone();
                applicationMessage.DisposePayload();
            }
        }
    }

    public MqttApplicationMessage ApplicationMessage { get; private set; }

    /// <summary>
    ///     Gets or sets whether the library should send MQTT ACK packets automatically if required.
    /// </summary>
    public bool AutoAcknowledge { get; set; } = true;

    /// <summary>
    ///     Gets the client identifier.
    ///     Hint: This identifier needs to be unique over all used clients / devices on the broker to avoid connection issues.
    /// </summary>
    public string ClientId { get; }

    /// <summary>
    ///     Gets or sets whether the ownership of the message payload 
    ///     was handed over to the invoked code. This value determines
    ///     if the payload can be disposed after the callback returns.
    ///     If transferred, the new owner is responsible
    ///     to dispose the payload after processing.
    /// </summary>
    public bool TransferredPayload { get; private set; } = false;

    /// <summary>
    ///     Gets or sets whether this message was handled.
    ///     This value can be used in user code for custom control flow.
    /// </summary>
    public bool IsHandled { get; set; }

    /// <summary>
    ///     Gets the identifier of the MQTT packet
    /// </summary>
    public ushort PacketIdentifier => PublishPacket.PacketIdentifier;

    /// <summary>
    ///     Indicates if the processing of this PUBLISH packet has failed.
    ///     If the processing has failed the client will not send an ACK packet etc.
    /// </summary>
    public bool ProcessingFailed { get; set; }

    /// <summary>
    ///     Gets or sets the reason code which will be sent to the server.
    /// </summary>
    public MqttApplicationMessageReceivedReasonCode ReasonCode { get; set; } = MqttApplicationMessageReceivedReasonCode.Success;

    /// <summary>
    ///     Gets or sets the reason string which will be sent to the server in the ACK packet.
    /// </summary>
    public string ResponseReasonString { get; set; }

    /// <summary>
    ///     Gets or sets the user properties which will be sent to the server in the ACK packet etc.
    /// </summary>
    public List<MqttUserProperty> ResponseUserProperties { get; } = new();

    public object Tag { get; set; }

    internal MqttPublishPacket PublishPacket { get; set; }

    public Task AcknowledgeAsync(CancellationToken cancellationToken)
    {
        if (_acknowledgeHandler == null)
        {
            throw new NotSupportedException("Deferred acknowledgement of application message is not yet supported in MQTTnet server.");
        }

        if (Interlocked.CompareExchange(ref _isAcknowledged, 1, 0) == 0)
        {
            return _acknowledgeHandler(this, cancellationToken);
        }

        throw new InvalidOperationException("The application message is already acknowledged.");
    }
}