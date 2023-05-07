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
    public sealed class MqttInitialAuthenticationStrategy : IMqttAuthenticationTransportStrategy
    {
        readonly IMqttChannelAdapter _channelAdapter;

        public MqttInitialAuthenticationStrategy(IMqttChannelAdapter channelAdapter)
        {
            _channelAdapter = channelAdapter ?? throw new ArgumentNullException(nameof(channelAdapter));
        }


        public Task SendPacketAsync(MqttAuthPacket authPacket, CancellationToken cancellationToken)
        {
            if (authPacket == null)
            {
                throw new ArgumentNullException(nameof(authPacket));
            }

            return _channelAdapter.SendPacketAsync(authPacket, cancellationToken);
        }

        public async Task<MqttAuthPacket> ReceivePacketAsync(string authenticationMethod, CancellationToken cancellationToken)
        {
            if (authenticationMethod == null)
            {
                throw new ArgumentNullException(nameof(authenticationMethod));
            }

            var receivePacket = await _channelAdapter.ReceivePacketAsync(cancellationToken).ConfigureAwait(false);
            if (receivePacket is MqttAuthPacket authPacket)
            {
                if (!string.Equals(authPacket.AuthenticationMethod, authenticationMethod, StringComparison.Ordinal))
                {
                    throw new MqttProtocolViolationException("The authentication method is not allowed to change while authenticating.");
                }

                return authPacket;
            }

            if (receivePacket == null)
            {
                throw new MqttCommunicationException("The server closed the connection.");
            }

            throw new MqttProtocolViolationException("Expected an AUTH packet from the client.");
        }
    }
    
    public interface IMqttAuthenticationTransportStrategy
    {
        Task SendPacketAsync(MqttAuthPacket authPacket, CancellationToken cancellationToken);
        
        Task<MqttAuthPacket> ReceivePacketAsync(string authenticationMethod, CancellationToken cancellationToken);
    }
    
    public sealed class MqttClientAuthenticationHandler
    {
        readonly MqttClient _client;

        readonly AsyncEvent<MqttExtendedAuthenticationExchangeEventArgs> _extendedAuthenticationExchangeEvent = new AsyncEvent<MqttExtendedAuthenticationExchangeEventArgs>();
        readonly MqttNetSourceLogger _logger;

        public MqttClientAuthenticationHandler(MqttClient client, IMqttNetLogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger.WithSource(nameof(MqttClientAuthenticationHandler));
        }

        public void AddHandler(Func<MqttExtendedAuthenticationExchangeEventArgs, Task> handler)
        {
            _extendedAuthenticationExchangeEvent.AddHandler(handler);
        }

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
                await _client.Send(connectPacket, cancellationToken).ConfigureAwait(false);

                // Wait for the CONNACK packet...
                MqttConnAckPacket connAckPacket = null;
                while (!cancellationToken.IsCancellationRequested)
                {
                    var receivedPacket = await channelAdapter.ReceivePacketAsync(cancellationToken).ConfigureAwait(false);

                    if (receivedPacket is MqttAuthPacket initialAuthPacket)
                    {
                        // MQTT v3.1.1 cannot send an AUTH packet.
                        // If the Server requires additional information to complete the authentication, it can send an AUTH packet to the Client.
                        // This packet MUST contain a Reason Code of 0x18 (Continue authentication)
                        if (initialAuthPacket.ReasonCode != MqttAuthenticateReasonCode.ContinueAuthentication)
                        {
                            throw new MqttProtocolViolationException("Wrong reason code received [MQTT-4.12.0-2].");
                        }

                        //var response = await HandleExtendedAuthentication(authPacket, channelAdapter).ConfigureAwait(false);
                        await HandleExtendedAuthentication(initialAuthPacket, new MqttInitialAuthenticationStrategy(channelAdapter)).ConfigureAwait(false);
                    }
                    else if (receivedPacket is MqttConnAckPacket connAckPacketBuffer)
                    {
                        connAckPacket = connAckPacketBuffer;
                        
                        // The CONNACK packet is the last packet when authenticating so there is no further AUTH packet allowed!
                        break;
                    }
                    else
                    {
                        throw new MqttProtocolViolationException($"Received {receivedPacket.GetRfcName()} while authenticating.");
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();

                // If the initial CONNECT packet included an Authentication Method property then all AUTH packets,
                // and any successful CONNACK packet MUST include an Authentication Method Property with the same
                // value as in the CONNECT packet [MQTT-4.12.0-5].
                if (!string.IsNullOrEmpty(connectPacket.AuthenticationMethod))
                {
                    if (!string.Equals(connectPacket.AuthenticationMethod, connAckPacket?.AuthenticationMethod))
                    {
                        throw new MqttProtocolViolationException("The CONNACK packet does not have the same authentication method as the CONNECT packet.");
                    }
                }

                result = MqttClientResultFactory.ConnectResult.Create(connAckPacket, channelAdapter.PacketFormatterAdapter.ProtocolVersion);
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
                _logger.Warning(
                    "Client will now throw an _MqttConnectingFailedException_. This is obsolete and will be removed in the future. Consider setting _ThrowOnNonSuccessfulResponseFromServer=False_ in client options.");

                if (result.ResultCode != MqttClientConnectResultCode.Success)
                {
                    throw new MqttConnectingFailedException($"Connecting with MQTT server failed ({result.ResultCode}).", null, result);
                }
            }

            _logger.Verbose("Authenticated MQTT connection with server established.");

            return result;
        }

        public Task HandleExtendedAuthentication(MqttAuthPacket initialAuthPacket, IMqttAuthenticationTransportStrategy transportStrategy)
        {
            ValidateEventHandler();

            var eventArgs = new MqttExtendedAuthenticationExchangeEventArgs(initialAuthPacket, transportStrategy);
            return _extendedAuthenticationExchangeEvent.InvokeAsync(eventArgs);
        }

        void ValidateEventHandler()
        {
            if (!_extendedAuthenticationExchangeEvent.HasHandlers)
            {
                throw new InvalidOperationException("Cannot handle extended authentication without attached event handler.");
            }
        }
        
        public void RemoveHandler(Func<MqttExtendedAuthenticationExchangeEventArgs, Task> handler)
        {
            _extendedAuthenticationExchangeEvent.RemoveHandler(handler);
        }

        public async Task ReAuthenticate(MqttReAuthenticationOptions options, CancellationToken cancellationToken)
        {
            ValidateEventHandler();
            
            var authPacket = MqttPacketFactories.Auth.CreateReAuthenticationPacket(_client.Options, options);

            
            
            // Sending the AUTH packet will trigger the re authentication and the event handler will
            // handle the details.
            await _client.Send(authPacket, cancellationToken).ConfigureAwait(false);
        }
    }
}