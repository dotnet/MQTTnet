using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography.Certificates;
using Windows.UI.Core;
using Windows.UI.Xaml;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Exceptions;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Extensions.Rpc;
using MQTTnet.Implementations;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MqttClientConnectedEventArgs = MQTTnet.Client.MqttClientConnectedEventArgs;
using MqttClientDisconnectedEventArgs = MQTTnet.Client.MqttClientDisconnectedEventArgs;

namespace MQTTnet.TestApp.UniversalWindows
{
    public sealed partial class MainPage
    {
        private readonly ConcurrentQueue<MqttNetLogMessage> _traceMessages = new ConcurrentQueue<MqttNetLogMessage>();
        private readonly ObservableCollection<IMqttClientSessionStatus> _sessions = new ObservableCollection<IMqttClientSessionStatus>();

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
            var tlsOptions = new MqttClientTlsOptions
            {
                UseTls = UseTls.IsChecked == true,
                IgnoreCertificateChainErrors = true,
                IgnoreCertificateRevocationErrors = true,
                AllowUntrustedCertificates = true
            };

            var options = new MqttClientOptions
            {
                ClientId = ClientId.Text
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

            if (options.ChannelOptions == null)
            {
                throw new InvalidOperationException();
            }

            options.Credentials = new MqttClientCredentials
            {
                Username = User.Text,
                Password = Password.Text
            };

            options.CleanSession = CleanSession.IsChecked == true;
            options.KeepAlivePeriod = TimeSpan.FromSeconds(double.Parse(KeepAliveInterval.Text));
            
            try
            {
                if (_mqttClient != null)
                {
                    await _mqttClient.DisconnectAsync();
                    _mqttClient.ApplicationMessageReceived -= OnApplicationMessageReceived;
                    _mqttClient.Connected -= OnConnected;
                    _mqttClient.Disconnected -= OnDisconnected;
                }

                var factory = new MqttFactory();

                if (UseManagedClient.IsChecked == true)
                {
                    _managedMqttClient = factory.CreateManagedMqttClient();
                    _managedMqttClient.ApplicationMessageReceived += OnApplicationMessageReceived;
                    _managedMqttClient.Connected += OnConnected;
                    _managedMqttClient.Disconnected += OnDisconnected;

                    await _managedMqttClient.StartAsync(new ManagedMqttClientOptions
                    {
                        ClientOptions = options
                    });
                }
                else
                {
                    _mqttClient = factory.CreateMqttClient();
                    _mqttClient.ApplicationMessageReceived += OnApplicationMessageReceived;
                    _mqttClient.Connected += OnConnected;
                    _mqttClient.Disconnected += OnDisconnected;

                    await _mqttClient.ConnectAsync(options);
                }
            }
            catch (Exception exception)
            {
                Trace.Text += exception + Environment.NewLine;
            }
        }

        private void OnDisconnected(object sender, MqttClientDisconnectedEventArgs e)
        {
            _traceMessages.Enqueue(new MqttNetLogMessage("", DateTime.Now, -1,
                "", MqttNetLogLevel.Info, "! DISCONNECTED EVENT FIRED", null));

            Task.Run(UpdateLogAsync);
        }

        private void OnConnected(object sender, MqttClientConnectedEventArgs e)
        {
            _traceMessages.Enqueue(new MqttNetLogMessage("", DateTime.Now, -1,
                "", MqttNetLogLevel.Info, "! CONNECTED EVENT FIRED", null));

            Task.Run(UpdateLogAsync);
        }

        private async void OnApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            var item = $"Timestamp: {DateTime.Now:O} | Topic: {eventArgs.ApplicationMessage.Topic} | Payload: {Encoding.UTF8.GetString(eventArgs.ApplicationMessage.Payload)} | QoS: {eventArgs.ApplicationMessage.QualityOfServiceLevel}";

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

                if (_mqttClient != null)
                {
                    await _mqttClient.SubscribeAsync(new TopicFilter(SubscribeTopic.Text, qos));
                }

                if (_managedMqttClient != null)
                {
                    await _managedMqttClient.SubscribeAsync(new TopicFilter(SubscribeTopic.Text, qos));
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

        private async void RefreshSessions(object sender, RoutedEventArgs e)
        {
            if (_mqttServer == null)
            {
                return;
            }

            var sessions = await _mqttServer.GetClientSessionsStatusAsync();
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
                // Write all trace messages to the console window.
                MqttNetGlobalLogger.LogMessagePublished += (s, e) =>
                {
                    Console.WriteLine($">> [{e.TraceMessage.Timestamp:O}] [{e.TraceMessage.ThreadId}] [{e.TraceMessage.Source}] [{e.TraceMessage.Level}]: {e.TraceMessage.Message}");
                    if (e.TraceMessage.Exception != null)
                    {
                        Console.WriteLine(e.TraceMessage.Exception);
                    }
                };
            }

            {
                // Use a custom identifier for the trace messages.
                var clientOptions = new MqttClientOptionsBuilder()
                    .Build();
            }

            {
                // Create a new MQTT client.
                var factory = new MqttFactory();
                var mqttClient = factory.CreateMqttClient();

                {
                    // Create TCP based options using the builder.
                    var options = new MqttClientOptionsBuilder()
                        .WithClientId("Client1")
                        .WithTcpServer("broker.hivemq.com")
                        .WithCredentials("bud", "%spencer%")
                        .WithTls()
                        .WithCleanSession()
                        .Build();

                    await mqttClient.ConnectAsync(options);
                }

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

                    await mqttClient.ConnectAsync(options);
                }

                {
                    // Create TCP based options manually
                    var options = new MqttClientOptions
                    {
                        ClientId = "Client1",
                        Credentials = new MqttClientCredentials
                        {
                            Username = "bud",
                            Password = "%spencer%"
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

                {
                    // Subscribe to a topic
                    await mqttClient.SubscribeAsync(new TopicFilterBuilder().WithTopic("my/topic").Build());

                    // Unsubscribe from a topic
                    await mqttClient.UnsubscribeAsync("my/topic");

                    // Publish an application message
                    var applicationMessage = new MqttApplicationMessageBuilder()
                        .WithTopic("A/B/C")
                        .WithPayload("Hello World")
                        .WithAtLeastOnceQoS()
                        .Build();

                    await mqttClient.PublishAsync(applicationMessage);
                }
            }

            // ----------------------------------
            {
                var options = new MqttServerOptions();

                options.ConnectionValidator = c =>
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
                };

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

                options.ConnectionValidator = c =>
                {
                    if (c.ClientId != "Highlander")
                    {
                        c.ReturnCode = MqttConnectReturnCode.ConnectionRefusedIdentifierRejected;
                        return;
                    }

                    c.ReturnCode = MqttConnectReturnCode.ConnectionAccepted;
                };

                var mqttServer = new MqttFactory().CreateMqttServer();
                await mqttServer.StartAsync(optionsBuilder.Build());
            }

            {
                // Setup client validator.
                var options = new MqttServerOptions
                {
                    ConnectionValidator = c =>
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
                    }
                };
            }

            {
                // Create a new MQTT server.
                var mqttServer = new MqttFactory().CreateMqttServer();
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
        }

        #endregion
    }
}
