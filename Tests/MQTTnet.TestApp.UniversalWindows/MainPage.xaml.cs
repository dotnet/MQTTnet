using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Diagnostics;
using MQTTnet.Exceptions;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Extensions.Rpc;
using MQTTnet.Extensions.WebSocket4Net;
using MQTTnet.Formatter;
using MQTTnet.Implementations;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MQTTnet.Server.Status;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography.Certificates;
using Windows.UI.Core;
using Windows.UI.Xaml;
using MqttClientConnectedEventArgs = MQTTnet.Client.Connecting.MqttClientConnectedEventArgs;
using MqttClientDisconnectedEventArgs = MQTTnet.Client.Disconnecting.MqttClientDisconnectedEventArgs;

namespace MQTTnet.TestApp.UniversalWindows
{
    public sealed partial class MainPage
    {
        private readonly ConcurrentQueue<MqttNetLogMessage> _traceMessages = new ConcurrentQueue<MqttNetLogMessage>();
        private readonly ObservableCollection<IMqttClientStatus> _sessions = new ObservableCollection<IMqttClientStatus>();

        private IMqttClient _mqttClient;
        private IManagedMqttClient _managedMqttClient;
        private IMqttServer _mqttServer;

        public MainPage()
        {
            InitializeComponent();

            ClientId.Text = Guid.NewGuid().ToString("D");

            MqttNetGlobalLogger.LogMessagePublished += OnTraceMessagePublished;
        }

        private async void OnTraceMessagePublished(object sender, MqttNetLogMessagePublishedEventArgs e)
        {
            _traceMessages.Enqueue(e.TraceMessage);
            await UpdateLogAsync();
        }

        private async Task UpdateLogAsync()
        {
            while (_traceMessages.Count > 100)
            {
                _traceMessages.TryDequeue(out _);
            }

            var logText = new StringBuilder();
            foreach (var traceMessage in _traceMessages)
            {
                logText.AppendFormat(
                    "[{0:yyyy-MM-dd HH:mm:ss.fff}] [{1}] [{2}] [{3}] [{4}]{5}",
                    traceMessage.Timestamp,
                    traceMessage.Level,
                    traceMessage.Source,
                    traceMessage.ThreadId,
                    traceMessage.Message,
                    Environment.NewLine);

                if (traceMessage.Exception != null)
                {
                    logText.AppendLine(traceMessage.Exception.ToString());
                }
            }

            await Trace.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                Trace.Text = logText.ToString();
            });
        }

        private async void Connect(object sender, RoutedEventArgs e)
        {
            var mqttFactory = new MqttFactory();

            var tlsOptions = new MqttClientTlsOptions
            {
                UseTls = UseTls.IsChecked == true,
                IgnoreCertificateChainErrors = true,
                IgnoreCertificateRevocationErrors = true,
                AllowUntrustedCertificates = true
            };

            var options = new MqttClientOptions
            {
                ClientId = ClientId.Text,
                ProtocolVersion = MqttProtocolVersion.V500
            };

            if (UseTcp.IsChecked == true)
            {
                options.ChannelOptions = new MqttClientTcpOptions
                {
                    Server = Server.Text,
                    Port = int.Parse(Port.Text),
                    TlsOptions = tlsOptions
                };
            }

            if (UseWs.IsChecked == true)
            {
                options.ChannelOptions = new MqttClientWebSocketOptions
                {
                    Uri = Server.Text,
                    TlsOptions = tlsOptions
                };
            }

            if (UseWs4Net.IsChecked == true)
            {
                options.ChannelOptions = new MqttClientWebSocketOptions
                {
                    Uri = Server.Text,
                    TlsOptions = tlsOptions
                };

                mqttFactory.UseWebSocket4Net();
            }

            if (options.ChannelOptions == null)
            {
                throw new InvalidOperationException();
            }

            if (!string.IsNullOrEmpty(User.Text))
            {
                options.Credentials = new MqttClientCredentials
                {
                    Username = User.Text,
                    Password = Encoding.UTF8.GetBytes(Password.Text)
                };
            }

            options.CleanSession = CleanSession.IsChecked == true;
            options.KeepAlivePeriod = TimeSpan.FromSeconds(double.Parse(KeepAliveInterval.Text));

            if (UseMqtt310.IsChecked == true)
            {
                options.ProtocolVersion = MqttProtocolVersion.V310;
            }
            else if (UseMqtt311.IsChecked == true)
            {
                options.ProtocolVersion = MqttProtocolVersion.V311;
            }
            else if (UseMqtt500.IsChecked == true)
            {
                options.ProtocolVersion = MqttProtocolVersion.V500;
            }

            try
            {
                if (_mqttClient != null)
                {
                    await _mqttClient.DisconnectAsync();
                    _mqttClient.UseApplicationMessageReceivedHandler(HandleReceivedApplicationMessage);
                    _mqttClient.ConnectedHandler = new MqttClientConnectedHandlerDelegate(x => OnConnected(x));
                    _mqttClient.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(x => OnDisconnected(x));
                }

                if (UseManagedClient.IsChecked == true)
                {
                    _managedMqttClient = mqttFactory.CreateManagedMqttClient();
                    _managedMqttClient.UseApplicationMessageReceivedHandler(HandleReceivedApplicationMessage);
                    _managedMqttClient.ConnectedHandler = new MqttClientConnectedHandlerDelegate(x => OnConnected(x));
                    _managedMqttClient.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(x => OnDisconnected(x));

                    await _managedMqttClient.StartAsync(new ManagedMqttClientOptions
                    {
                        ClientOptions = options
                    });
                }
                else
                {
                    _mqttClient = mqttFactory.CreateMqttClient();
                    _mqttClient.UseApplicationMessageReceivedHandler(HandleReceivedApplicationMessage);
                    _mqttClient.ConnectedHandler = new MqttClientConnectedHandlerDelegate(x => OnConnected(x));
                    _mqttClient.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(x => OnDisconnected(x));

                    await _mqttClient.ConnectAsync(options);
                }
            }
            catch (Exception exception)
            {
                Trace.Text += exception + Environment.NewLine;
            }
        }

        private void OnDisconnected(MqttClientDisconnectedEventArgs e)
        {
            _traceMessages.Enqueue(new MqttNetLogMessage
            {
                Timestamp = DateTime.UtcNow,
                ThreadId = -1,
                Level = MqttNetLogLevel.Info,
                Message = "! DISCONNECTED EVENT FIRED",
            });

            Task.Run(UpdateLogAsync);
        }

        private void OnConnected(MqttClientConnectedEventArgs e)
        {
            _traceMessages.Enqueue(new MqttNetLogMessage
            {
                Timestamp = DateTime.UtcNow,
                ThreadId = -1,
                Level = MqttNetLogLevel.Info,
                Message = "! CONNECTED EVENT FIRED",
            });

            Task.Run(UpdateLogAsync);
        }

        private async Task HandleReceivedApplicationMessage(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            var item = $"Timestamp: {DateTime.Now:O} | Topic: {eventArgs.ApplicationMessage.Topic} | Payload: {eventArgs.ApplicationMessage.ConvertPayloadToString()} | QoS: {eventArgs.ApplicationMessage.QualityOfServiceLevel}";

            await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                if (AddReceivedMessagesToList.IsChecked == true)
                {
                    ReceivedMessages.Items.Add(item);
                }
            });
        }

        private async void Publish(object sender, RoutedEventArgs e)
        {
            try
            {
                var qos = MqttQualityOfServiceLevel.AtMostOnce;
                if (QoS1.IsChecked == true)
                {
                    qos = MqttQualityOfServiceLevel.AtLeastOnce;
                }

                if (QoS2.IsChecked == true)
                {
                    qos = MqttQualityOfServiceLevel.ExactlyOnce;
                }

                var payload = new byte[0];
                if (PlainText.IsChecked == true)
                {
                    payload = Encoding.UTF8.GetBytes(Payload.Text);
                }

                if (Base64.IsChecked == true)
                {
                    payload = Convert.FromBase64String(Payload.Text);
                }

                var message = new MqttApplicationMessageBuilder()
                    .WithContentType(ContentType.Text)
                    .WithResponseTopic(ResponseTopic.Text)
                    .WithTopic(Topic.Text)
                    .WithPayload(payload)
                    .WithQualityOfServiceLevel(qos)
                    .WithRetainFlag(Retain.IsChecked == true)
                    .Build();

                if (_mqttClient != null)
                {
                    await _mqttClient.PublishAsync(message);
                }

                if (_managedMqttClient != null)
                {
                    await _managedMqttClient.PublishAsync(message);
                }
            }
            catch (Exception exception)
            {
                Trace.Text += exception + Environment.NewLine;
            }
        }

        private async void Disconnect(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_mqttClient != null)
                {
                    await _mqttClient.DisconnectAsync();
                    _mqttClient.Dispose();
                    _mqttClient = null;
                }

                if (_managedMqttClient != null)
                {
                    await _managedMqttClient.StopAsync();
                    _managedMqttClient.Dispose();
                    _managedMqttClient = null;
                }
            }
            catch (Exception exception)
            {
                Trace.Text += exception + Environment.NewLine;
            }
        }

        private void ClearLog(object sender, RoutedEventArgs e)
        {
            while (_traceMessages.Count > 0)
            {
                _traceMessages.TryDequeue(out _);
            }

            Trace.Text = string.Empty;
        }

        private async void Subscribe(object sender, RoutedEventArgs e)
        {
            try
            {
                var qos = MqttQualityOfServiceLevel.AtMostOnce;
                if (SubscribeQoS1.IsChecked == true)
                {
                    qos = MqttQualityOfServiceLevel.AtLeastOnce;
                }

                if (SubscribeQoS2.IsChecked == true)
                {
                    qos = MqttQualityOfServiceLevel.ExactlyOnce;
                }

                var topicFilter = new TopicFilter { Topic = SubscribeTopic.Text, QualityOfServiceLevel = qos };

                if (_mqttClient != null)
                {
                    await _mqttClient.SubscribeAsync(topicFilter);
                }

                if (_managedMqttClient != null)
                {
                    await _managedMqttClient.SubscribeAsync(topicFilter);
                }
            }
            catch (Exception exception)
            {
                Trace.Text += exception + Environment.NewLine;
            }
        }

        private async void Unsubscribe(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_mqttClient != null)
                {
                    await _mqttClient.UnsubscribeAsync(SubscribeTopic.Text);
                }

                if (_managedMqttClient != null)
                {
                    await _managedMqttClient.UnsubscribeAsync(SubscribeTopic.Text);
                }
            }
            catch (Exception exception)
            {
                Trace.Text += exception + Environment.NewLine;
            }
        }

        // This code is for the Wiki at GitHub!
        // ReSharper disable once UnusedMember.Local

        private async void StartServer(object sender, RoutedEventArgs e)
        {
            if (_mqttServer != null)
            {
                return;
            }

            JsonServerStorage storage = null;
            if (ServerPersistRetainedMessages.IsChecked == true)
            {
                storage = new JsonServerStorage();

                if (ServerClearRetainedMessages.IsChecked == true)
                {
                    storage.Clear();
                }
            }

            _mqttServer = new MqttFactory().CreateMqttServer();

            var options = new MqttServerOptions();
            options.DefaultEndpointOptions.Port = int.Parse(ServerPort.Text);
            options.Storage = storage;
            options.EnablePersistentSessions = ServerAllowPersistentSessions.IsChecked == true;

            await _mqttServer.StartAsync(options);
        }

        private async void StopServer(object sender, RoutedEventArgs e)
        {
            if (_mqttServer == null)
            {
                return;
            }

            await _mqttServer.StopAsync();
            _mqttServer = null;
        }

        private void ClearReceivedMessages(object sender, RoutedEventArgs e)
        {
            ReceivedMessages.Items.Clear();
        }

        private async void ExecuteRpc(object sender, RoutedEventArgs e)
        {
            var qos = MqttQualityOfServiceLevel.AtMostOnce;
            if (RpcQoS1.IsChecked == true)
            {
                qos = MqttQualityOfServiceLevel.AtLeastOnce;
            }

            if (RpcQoS2.IsChecked == true)
            {
                qos = MqttQualityOfServiceLevel.ExactlyOnce;
            }

            var payload = new byte[0];
            if (RpcText.IsChecked == true)
            {
                payload = Encoding.UTF8.GetBytes(RpcPayload.Text);
            }

            if (RpcBase64.IsChecked == true)
            {
                payload = Convert.FromBase64String(RpcPayload.Text);
            }

            try
            {
                var rpcClient = new MqttRpcClient(_mqttClient);
                var response = await rpcClient.ExecuteAsync(TimeSpan.FromSeconds(5), RpcMethod.Text, payload, qos);

                RpcResponses.Items.Add(RpcMethod.Text + " >>> " + Encoding.UTF8.GetString(response));
            }
            catch (MqttCommunicationTimedOutException)
            {
                RpcResponses.Items.Add(RpcMethod.Text + " >>> [TIMEOUT]");
            }
            catch (Exception exception)
            {
                RpcResponses.Items.Add(RpcMethod.Text + " >>> [EXCEPTION (" + exception.Message + ")]");
            }
        }

        private void ClearRpcResponses(object sender, RoutedEventArgs e)
        {
            RpcResponses.Items.Clear();
        }

        private void ClearSessions(object sender, RoutedEventArgs e)
        {
            _sessions.Clear();
        }

        private void RefreshSessions(object sender, RoutedEventArgs e)
        {
            if (_mqttServer == null)
            {
                return;
            }

            var sessions = _mqttServer.GetClientStatusAsync().GetAwaiter().GetResult();
            _sessions.Clear();

            foreach (var session in sessions)
            {
                _sessions.Add(session);
            }

            ListViewSessions.DataContext = _sessions;
        }

        #region Wiki Code

        private async Task WikiCode()
        {
            {
                // Use a custom identifier for the trace messages.
                var clientOptions = new MqttClientOptionsBuilder()
                    .Build();
            }

            {
                // Create a new MQTT client.
                var factory = new MqttFactory();
                var client = factory.CreateMqttClient();

                // Create TCP based options using the builder.
                var options = new MqttClientOptionsBuilder()
                    .WithClientId("Client1")
                    .WithTcpServer("broker.hivemq.com")
                    .WithCredentials("bud", "%spencer%")
                    .WithTls()
                    .WithCleanSession()
                    .Build();

                await client.ConnectAsync(options);

                // Reconnecting

                client.UseDisconnectedHandler(async e =>
                {
                    Console.WriteLine("### DISCONNECTED FROM SERVER ###");
                    await Task.Delay(TimeSpan.FromSeconds(5));

                    try
                    {
                        await client.ConnectAsync(options);
                    }
                    catch
                    {
                        Console.WriteLine("### RECONNECTING FAILED ###");
                    }
                });

                // Consuming messages

                client.UseApplicationMessageReceivedHandler(e =>
                {
                    Console.WriteLine("### RECEIVED APPLICATION MESSAGE ###");
                    Console.WriteLine($"+ Topic = {e.ApplicationMessage.Topic}");
                    Console.WriteLine($"+ Payload = {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}");
                    Console.WriteLine($"+ QoS = {e.ApplicationMessage.QualityOfServiceLevel}");
                    Console.WriteLine($"+ Retain = {e.ApplicationMessage.Retain}");
                    Console.WriteLine();
                });

                void Handler(MqttApplicationMessageReceivedEventArgs args)
                {
                    //...
                }

                client.UseApplicationMessageReceivedHandler(e => Handler(e));

                // Subscribe after connect

                client.UseConnectedHandler(async e =>
                {
                    Console.WriteLine("### CONNECTED WITH SERVER ###");

                    // Subscribe to a topic
                    await client.SubscribeAsync(new TopicFilterBuilder().WithTopic("my/topic").Build());

                    Console.WriteLine("### SUBSCRIBED ###");
                });

                // Subscribe to a topic
                await client.SubscribeAsync(new TopicFilterBuilder().WithTopic("my/topic").Build());

                // Unsubscribe from a topic
                await client.UnsubscribeAsync("my/topic");

                // Publish an application message
                var applicationMessage = new MqttApplicationMessageBuilder()
                    .WithTopic("A/B/C")
                    .WithPayload("Hello World")
                    .WithAtLeastOnceQoS()
                    .Build();

                await client.PublishAsync(applicationMessage);
            }

            {

                {
                    // Use TCP connection.
                    var options = new MqttClientOptionsBuilder()
                        .WithTcpServer("broker.hivemq.com", 1883) // Port is optional
                        .Build();
                }

                {
                    // Use secure TCP connection.
                    var options = new MqttClientOptionsBuilder()
                        .WithTcpServer("broker.hivemq.com")
                        .WithTls()
                        .Build();
                }

                {
                    // Use WebSocket connection.
                    var options = new MqttClientOptionsBuilder()
                        .WithWebSocketServer("broker.hivemq.com:8000/mqtt")
                        .Build();
                }

                {
                    // Create TCP based options manually
                    var options = new MqttClientOptions
                    {
                        ClientId = "Client1",
                        Credentials = new MqttClientCredentials
                        {
                            Username = "bud",
                            Password = Encoding.UTF8.GetBytes("%spencer%")
                        },
                        ChannelOptions = new MqttClientTcpOptions
                        {
                            Server = "broker.hivemq.org",
                            TlsOptions = new MqttClientTlsOptions
                            {
                                UseTls = true
                            }
                        },
                    };
                }
            }

            // ----------------------------------
            {
                var options = new MqttServerOptions();

                options.ConnectionValidator = new MqttServerConnectionValidatorDelegate(c =>
                {
                    if (c.ClientId.Length < 10)
                    {
                        c.ReturnCode = MqttConnectReturnCode.ConnectionRefusedIdentifierRejected;
                        return;
                    }

                    if (c.Username != "mySecretUser")
                    {
                        c.ReturnCode = MqttConnectReturnCode.ConnectionRefusedBadUsernameOrPassword;
                        return;
                    }

                    if (c.Password != "mySecretPassword")
                    {
                        c.ReturnCode = MqttConnectReturnCode.ConnectionRefusedBadUsernameOrPassword;
                        return;
                    }

                    c.ReturnCode = MqttConnectReturnCode.ConnectionAccepted;
                });

                var factory = new MqttFactory();
                var mqttServer = factory.CreateMqttServer();
                await mqttServer.StartAsync(options);

                Console.WriteLine("Press any key to exit.");
                Console.ReadLine();

                await mqttServer.StopAsync();
            }

            // ----------------------------------

            // For UWP apps:
            MqttTcpChannel.CustomIgnorableServerCertificateErrorsResolver = o =>
            {
                if (o.Server == "server_with_revoked_cert")
                {
                    return new[] { ChainValidationResult.Revoked };
                }

                return new ChainValidationResult[0];
            };

            {
                // Start a MQTT server.
                var mqttServer = new MqttFactory().CreateMqttServer();
                await mqttServer.StartAsync(new MqttServerOptions());
                Console.WriteLine("Press any key to exit.");
                Console.ReadLine();
                await mqttServer.StopAsync();
            }

            {
                // Configure MQTT server.
                var optionsBuilder = new MqttServerOptionsBuilder()
                    .WithConnectionBacklog(100)
                    .WithDefaultEndpointPort(1884);

                var options = new MqttServerOptions
                {
                };

                options.ConnectionValidator = new MqttServerConnectionValidatorDelegate(c =>
                {
                    if (c.ClientId != "Highlander")
                    {
                        c.ReturnCode = MqttConnectReturnCode.ConnectionRefusedIdentifierRejected;
                        return;
                    }

                    c.ReturnCode = MqttConnectReturnCode.ConnectionAccepted;
                });

                var mqttServer = new MqttFactory().CreateMqttServer();
                await mqttServer.StartAsync(optionsBuilder.Build());
            }

            {
                // Setup client validator.
                var options = new MqttServerOptions
                {
                    ConnectionValidator = new MqttServerConnectionValidatorDelegate(c =>
                    {
                        if (c.ClientId.Length < 10)
                        {
                            c.ReturnCode = MqttConnectReturnCode.ConnectionRefusedIdentifierRejected;
                            return;
                        }

                        if (c.Username != "mySecretUser")
                        {
                            c.ReturnCode = MqttConnectReturnCode.ConnectionRefusedBadUsernameOrPassword;
                            return;
                        }

                        if (c.Password != "mySecretPassword")
                        {
                            c.ReturnCode = MqttConnectReturnCode.ConnectionRefusedBadUsernameOrPassword;
                            return;
                        }

                        c.ReturnCode = MqttConnectReturnCode.ConnectionAccepted;
                    })
                };
            }

            {
                // Create a new MQTT server.
                var mqttServer = new MqttFactory().CreateMqttServer();
            }

            {
                // Setup application message interceptor.
                var options = new MqttServerOptionsBuilder()
                    .WithApplicationMessageInterceptor(context =>
                    {
                        if (context.ApplicationMessage.Topic == "my/custom/topic")
                        {
                            context.ApplicationMessage.Payload = Encoding.UTF8.GetBytes("The server injected payload.");
                        }

                        // It is also possible to read the payload and extend it. For example by adding a timestamp in a JSON document.
                        // This is useful when the IoT device has no own clock and the creation time of the message might be important.
                    })
                    .Build();
            }

            {
                // Setup subscription interceptor.
                var options = new MqttServerOptionsBuilder()
                    .WithSubscriptionInterceptor(context =>
                    {
                        if (context.TopicFilter.Topic.StartsWith("admin/foo/bar") && context.ClientId != "theAdmin")
                        {
                            context.AcceptSubscription = false;
                        }

                        if (context.TopicFilter.Topic.StartsWith("the/secret/stuff") && context.ClientId != "Imperator")
                        {
                            context.AcceptSubscription = false;
                            context.CloseConnection = true;
                        }
                    })
                    .Build();
            }

            {
                // Setup and start a managed MQTT client.
                var options = new ManagedMqttClientOptionsBuilder()
                    .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                    .WithClientOptions(new MqttClientOptionsBuilder()
                        .WithClientId("Client1")
                        .WithTcpServer("broker.hivemq.com")
                        .WithTls().Build())
                    .Build();

                var mqttClient = new MqttFactory().CreateManagedMqttClient();
                await mqttClient.SubscribeAsync(new TopicFilterBuilder().WithTopic("my/topic").Build());
                await mqttClient.StartAsync(options);
            }

            {
                // Use a custom log ID for the logger.
                var factory = new MqttFactory();
                var client = factory.CreateMqttClient(new MqttNetLogger("MyCustomId"));
            }

            {
                var client = new MqttFactory().CreateMqttClient();

                var message = new MqttApplicationMessageBuilder()
                    .WithTopic("MyTopic")
                    .WithPayload("Hello World")
                    .WithExactlyOnceQoS()
                    .WithRetainFlag()
                    .Build();

                await client.PublishAsync(message);
            }

            {
                // Write all trace messages to the console window.
                MqttNetGlobalLogger.LogMessagePublished += (s, e) =>
                {
                    var trace = $">> [{e.TraceMessage.Timestamp:O}] [{e.TraceMessage.ThreadId}] [{e.TraceMessage.Source}] [{e.TraceMessage.Level}]: {e.TraceMessage.Message}";
                    if (e.TraceMessage.Exception != null)
                    {
                        trace += Environment.NewLine + e.TraceMessage.Exception.ToString();
                    }

                    Console.WriteLine(trace);
                };
            }
        }

        #endregion
    }
}
