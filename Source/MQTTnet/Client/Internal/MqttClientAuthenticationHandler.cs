// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Client.Internal
{
    public sealed class MqttClientAuthenticationHandler
    {
        readonly MqttClient _client;
        readonly MqttNetSourceLogger _logger;
        readonly MqttClientConnectResultFactory _clientConnectResultFactory = new MqttClientConnectResultFactory();

        public MqttClientAuthenticationHandler(MqttClient client, IMqttNetLogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger.WithSource(nameof(MqttClientAuthenticationHandler));
        }

        public AsyncEvent<MqttExtendedAuthenticationExchangeEventArgs> ExtendedAuthenticationExchangeEvent { get; } = new AsyncEvent<MqttExtendedAuthenticationExchangeEventArgs>();

        public async Task<MqttClientConnectResult> Authenticate(IMqttChannelAdapter channelAdapter, MqttClientOptions options, CancellationToken cancellationToken)
        {
            if (channelAdapter == null)
            {
                throw new ArgumentNullException(nameof(channelAdapter));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            // From the RFC:
            // The CONNACK packet is the packet sent by the Server in response to a CONNECT packet received from a Client.
            // The Server MUST send a CONNACK with a 0x00 (Success) Reason Code before sending any Packet other than AUTH [MQTT-3.2.0-1].
            // The Server MUST NOT send more than one CONNACK in a Network Connection [MQTT-3.2.0-2].

            MqttClientConnectResult result;

            try
            {
                // Send the CONNECT packet..
                var connectPacket = MqttPacketFactories.Connect.Create(options);
                await _client.SendAsync(connectPacket, cancellationToken).ConfigureAwait(false);

                // Wait for the CONNACK packet...
                MqttConnAckPacket connAckPacket = null;
                while (!cancellationToken.IsCancellationRequested)
                {
                    var receivedPacket = await channelAdapter.ReceivePacketAsync(cancellationToken).ConfigureAwait(false);

                    if (receivedPacket is MqttAuthPacket authPacket)
                    {
                        // MQTT v3.1.1 cannot send an AUTH packet.
                        // If the Server requires additional information to complete the authentication, it can send an AUTH packet to the Client.
                        // This packet MUST contain a Reason Code of 0x18 (Continue authentication)
                        if (authPacket.ReasonCode != MqttAuthenticateReasonCode.ContinueAuthentication)
                        {
                            throw new MqttProtocolViolationException("Wrong reason code received [MQTT-4.12.0-2].");
                        }

                        var response = await OnExtendedAuthentication(authPacket).ConfigureAwait(false);
                        authPacket = MqttPacketFactories.Auth.Create(authPacket, response);
                        await _client.SendAsync(authPacket, cancellationToken).ConfigureAwait(false);
                    }
                    else if (receivedPacket is MqttConnAckPacket connAckPacketBuffer)
                    {
                        connAckPacket = connAckPacketBuffer;
                        break;
                    }
                }

                result = _clientConnectResultFactory.Create(connAckPacket, channelAdapter.PacketFormatterAdapter.ProtocolVersion);
            }
            catch (Exception exception)
            {
                throw new MqttConnectingFailedException($"Error while authenticating. {exception.Message}", exception, null);
            }

            // This is no feature. It is basically a backward compatibility option and should be removed in the future.
            // The client should not throw any exception if the transport layer connection was successful and the server
            // did send a proper ACK packet.
            if (options.ThrowOnNonSuccessfulResponseFromServer)
            {
                if (result.ResultCode != MqttClientConnectResultCode.Success)
                {
                    throw new MqttConnectingFailedException($"Connecting with MQTT server failed ({result.ResultCode}).", null, result);
                }
            }

            _logger.Verbose("Authenticated MQTT connection with server established.");

            return result;
        }

        async Task<MqttExtendedAuthenticationExchangeResponse> OnExtendedAuthentication(MqttAuthPacket authPacket)
        {
            if (!ExtendedAuthenticationExchangeEvent.HasHandlers)
            {
                throw new InvalidOperationException("Cannot handle extended authentication without attached event handler.");
            }

            var eventArgs = new MqttExtendedAuthenticationExchangeEventArgs(authPacket);
            await ExtendedAuthenticationExchangeEvent.InvokeAsync(eventArgs).ConfigureAwait(false);
            return eventArgs.Response;
        }
    }
}