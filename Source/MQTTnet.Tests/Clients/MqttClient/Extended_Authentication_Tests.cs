// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Formatter;
using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Tests.Clients.MqttClient
{
    [TestClass]
    public sealed class Extended_Authentication_Tests : BaseTestClass
    {
        [TestMethod]
        public async Task ReAuthenticate()
        {
            using (var testEnvironment = CreateTestEnvironment(MqttProtocolVersion.V500))
            {
                var server = await testEnvironment.StartServer();
                
                server.ValidatingConnectionAsync += async args =>
                {
                    using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                    {
                        // Just do a simple custom authentication.
                        await args.SendAuthenticationDataAsync(MqttAuthenticateReasonCode.ContinueAuthentication, cancellationToken: timeout.Token);
                        var clientResponse = await args.ReceiveAuthenticationDataAsync(timeout.Token);
                        CollectionAssert.AreEqual(clientResponse.AuthenticationData, Encoding.ASCII.GetBytes("TOKEN"));

                        var userProperties = new MqttUserPropertiesBuilder().With("x", "y").Build();
                        await args.SendAuthenticationDataAsync(MqttAuthenticateReasonCode.ContinueAuthentication, cancellationToken: timeout.Token, userProperties: userProperties);
                        
                        args.ReasonCode = MqttConnectReasonCode.Success;
                    }
                };

                server.ClientReAuthenticatingAsync += args =>
                {
                    
                    return CompletedTask.Instance;
                };
                
                var client = testEnvironment.CreateClient();
                client.ExtendedAuthenticationExchangeAsync += async args =>
                {
                    await args.SendAuthenticationDataAsync(MqttAuthenticateReasonCode.ContinueAuthentication, Encoding.ASCII.GetBytes("TOKEN"));

                    var serverResponse = await args.ReceiveAuthenticationDataAsync(CancellationToken.None);
                    Assert.IsNotNull(serverResponse.UserProperties);
                    Assert.AreEqual("x", serverResponse.UserProperties[0].Name);
                    Assert.AreEqual("y", serverResponse.UserProperties[0].Value);
                };

                var clientOptions = testEnvironment.CreateDefaultClientOptionsBuilder().WithTimeout(TimeSpan.FromSeconds(10)).WithAuthentication("CUSTOM").Build();
                await client.ConnectAsync(clientOptions);

                await LongTestDelay();

                await client.ReAuthenticateAsync();

                await LongTestDelay();

                var pingResult = await client.TryPingAsync();
                
                Assert.IsTrue(pingResult);
            }
        }
        
        [TestMethod]
        public async Task Use_Extended_Authentication()
        {
            var initialContextToken = Encoding.ASCII.GetBytes("initial context token");
            var replyContextToken = Encoding.ASCII.GetBytes("reply context token");
            var outcomeOfAuthentication = Encoding.ASCII.GetBytes("outcome of authentication");

            using (var testEnvironment = CreateTestEnvironment(MqttProtocolVersion.V500))
            {
                var server = await testEnvironment.StartServer();
                
                /*
                Sample flow from RFC:
                
                1. Client to Server CONNECT Authentication Method="GS2-KRB5"
                2. Server to Client AUTH rc=0x18 Authentication Method="GS2-KRB5"
                3. Client to Server AUTH rc=0x18 Authentication Method="GS2-KRB5" Authentication Data=initial context token
                4. Server to Client AUTH rc=0x18 Authentication Method="GS2-KRB5" Authentication Data=reply context token
                5. Client to Server AUTH rc=0x18 Authentication Method="GS2-KRB5"
                6. Server to Client CONNACK rc=0 Authentication Method="GS2-KRB5" Authentication Data=outcome of authentication
                 */

                server.ValidatingConnectionAsync += async args =>
                {
                    using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                    {
                        // 1.
                        if (args.AuthenticationMethod != "GS2-KRB5")
                        {
                            args.ReasonCode = MqttConnectReasonCode.BadAuthenticationMethod;
                            return;
                        }

                        // 2.
                        await args.SendAuthenticationDataAsync(MqttAuthenticateReasonCode.ContinueAuthentication, cancellationToken: timeout.Token);

                        // 3.
                        var clientResponse = await args.ReceiveAuthenticationDataAsync(timeout.Token);
                        CollectionAssert.AreEqual(clientResponse.AuthenticationData, initialContextToken);
                        Assert.AreEqual(MqttAuthenticateReasonCode.ContinueAuthentication, clientResponse.ReasonCode);

                        // 4.
                        await args.SendAuthenticationDataAsync(MqttAuthenticateReasonCode.ContinueAuthentication, replyContextToken, cancellationToken: timeout.Token);

                        // 5.
                        clientResponse = await args.ReceiveAuthenticationDataAsync(timeout.Token);
                        Assert.AreEqual(clientResponse.AuthenticationData, null);
                        Assert.AreEqual(MqttAuthenticateReasonCode.ContinueAuthentication, clientResponse.ReasonCode);

                        // 6.
                        args.ResponseAuthenticationData = outcomeOfAuthentication;
                        args.ReasonCode = MqttConnectReasonCode.Success;
                    }
                };

                var client = testEnvironment.CreateClient();
                client.ExtendedAuthenticationExchangeAsync += async args =>
                {
                    if (args.AuthenticationMethod != "GS2-KRB5")
                    {
                        Assert.Fail("Authentication method is wrong.");
                    }

                    // 2. THE FACT THAT THE SERVER SENDS THE AUTH PACKET WILL TRIGGER THIS EVENT SO THE INITIAL DATA IS IN THE EVENT ARGS!
                    Assert.AreEqual(MqttAuthenticateReasonCode.ContinueAuthentication, args.ReasonCode);
                    
                    // 3.
                    await args.SendAuthenticationDataAsync(MqttAuthenticateReasonCode.ContinueAuthentication, initialContextToken);

                    // 4.
                    var serverResponse = await args.ReceiveAuthenticationDataAsync(CancellationToken.None);
                    CollectionAssert.AreEqual(serverResponse.AuthenticationData, replyContextToken);
                    Assert.AreEqual(MqttAuthenticateReasonCode.ContinueAuthentication, serverResponse.ReasonCode);
                    
                    // 5.
                    await args.SendAuthenticationDataAsync(MqttAuthenticateReasonCode.ContinueAuthentication);
                };

                var clientOptions = testEnvironment.CreateDefaultClientOptionsBuilder().WithTimeout(TimeSpan.FromSeconds(10)).WithAuthentication("GS2-KRB5").Build();
                await client.ConnectAsync(clientOptions);
            }
        }
    }
}