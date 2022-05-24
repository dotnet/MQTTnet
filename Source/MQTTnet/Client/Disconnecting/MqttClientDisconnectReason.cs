// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet.Client
{
    public enum MqttClientDisconnectReason
    {
        NormalDisconnection = 0,
        DisconnectWithWillMessage = 4,
        UnspecifiedError = 128,
        MalformedPacket = 129,
        ProtocolError = 130,
        ImplementationSpecificError = 131,
        NotAuthorized = 135,
        ServerBusy = 137,
        ServerShuttingDown = 139,
        BadAuthenticationMethod = 140,
        KeepAliveTimeout = 141,
        SessionTakenOver = 142,
        TopicFilterInvalid = 143,
        TopicNameInvalid = 144,
        ReceiveMaximumExceeded = 147,
        TopicAliasInvalid = 148,
        PacketTooLarge = 149,
        MessageRateTooHigh = 150,
        QuotaExceeded = 151,
        AdministrativeAction = 152,
        PayloadFormatInvalid = 153,
        RetainNotSupported = 154,
        QosNotSupported = 155,
        UseAnotherServer = 156,
        ServerMoved = 157,
        SharedSubscriptionsNotSupported = 158,
        ConnectionRateExceeded = 159,
        MaximumConnectTime = 160,
        SubscriptionIdentifiersNotSupported = 161,
        WildcardSubscriptionsNotSupported = 162
    }
}
