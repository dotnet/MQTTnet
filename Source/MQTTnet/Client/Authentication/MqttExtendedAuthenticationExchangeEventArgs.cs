// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client.Internal;
using MQTTnet.Exceptions;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Client
{
    public sealed class MqttExtendedAuthenticationExchangeEventArgs : EventArgs
    {
        static readonly IReadOnlyCollection<MqttUserProperty> EmptyUserProperties = new List<MqttUserProperty>();

        readonly IMqttAuthenticationTransportStrategy _transportStrategy;

        public MqttExtendedAuthenticationExchangeEventArgs(MqttAuthPacket authPacket, IMqttAuthenticationTransportStrategy transportStrategy)
        {
            if (authPacket == null)
            {
                throw new ArgumentNullException(nameof(authPacket));
            }

            _transportStrategy = transportStrategy ?? throw new ArgumentNullException(nameof(transportStrategy));

            ReasonCode = authPacket.ReasonCode;
            ReasonString = authPacket.ReasonString;
            AuthenticationMethod = authPacket.AuthenticationMethod;
            AuthenticationData = authPacket.AuthenticationData;
            UserProperties = authPacket.UserProperties ?? EmptyUserProperties;
        }

        /// <summary>
        ///     Gets the authentication data.
        ///     <remarks>MQTT 5.0.0+ feature.</remarks>
        /// </summary>
        public byte[] AuthenticationData { get; }

        /// <summary>
        ///     Gets the authentication method.
        ///     <remarks>MQTT 5.0.0+ feature.</remarks>
        /// </summary>
        public string AuthenticationMethod { get; }

        /// <summary>
        ///     Gets the reason code.
        ///     <remarks>MQTT 5.0.0+ feature.</remarks>
        /// </summary>
        public MqttAuthenticateReasonCode ReasonCode { get; }

        /// <summary>
        ///     Gets the reason string.
        ///     <remarks>MQTT 5.0.0+ feature.</remarks>
        /// </summary>
        public string ReasonString { get; }

        // /// <summary>
        // ///     Gets the response which will be sent to the server.
        // /// </summary>
        // public MqttExtendedAuthenticationExchangeResponse Response { get; } = new MqttExtendedAuthenticationExchangeResponse();

        /// <summary>
        ///     Gets or sets the user properties.
        ///     <remarks>MQTT 5.0.0+ feature.</remarks>
        /// </summary>
        public IReadOnlyCollection<MqttUserProperty> UserProperties { get; }

        public async Task<MqttClientPartialAuthenticationResponse> ReceiveAuthenticationDataAsync(CancellationToken cancellationToken)
        {
            var receivePacket = await _transportStrategy.ReceivePacketAsync(AuthenticationMethod, cancellationToken).ConfigureAwait(false);
            if (receivePacket == null)
            {
                throw new MqttCommunicationException("The client closed the connection.");
            }

            return new MqttClientPartialAuthenticationResponse
            {
                ReasonCode = receivePacket.ReasonCode,
                AuthenticationData = receivePacket.AuthenticationData,
                UserProperties = receivePacket.UserProperties
            };
        }

        public Task SendAuthenticationDataAsync(
            MqttAuthenticateReasonCode reasonCode,
            byte[] authenticationData = null,
            string reasonString = null,
            List<MqttUserProperty> userProperties = null,
            CancellationToken cancellationToken = default)
        {
            // The authentication method will never change so we must use the already known one [MQTT-4.12.0-5].
            //var authPacket = MqttPacketFactories.Auth.Create(AuthenticationMethod, reasonCode);

            var authPacket = new MqttAuthPacket
            {
                AuthenticationMethod = AuthenticationMethod,
                AuthenticationData = authenticationData,
                UserProperties = userProperties,
                ReasonCode = reasonCode,
                ReasonString = reasonString
            };

            return _transportStrategy.SendPacketAsync(authPacket, cancellationToken);
        }
    }
}