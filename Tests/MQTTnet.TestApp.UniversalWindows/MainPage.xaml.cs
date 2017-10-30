using System;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography.Certificates;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Core;
using MQTTnet.Core.Client;
using MQTTnet.Core.Diagnostics;
using MQTTnet.Core.Protocol;
using MQTTnet.Core.Server;
using MQTTnet.Implementations;

namespace MQTTnet.TestApp.UniversalWindows
{
    public sealed partial class MainPage
    {
        private IMqttClient _mqttClient;
        private IMqttServer _mqttServer;

        public MainPage()
        {
            InitializeComponent();

            MqttNetTrace.TraceMessagePublished += OnTraceMessagePublished;
        }

        private async void OnTraceMessagePublished(object sender, MqttNetTraceMessagePublishedEventArgs e)
        {
            await Trace.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                var text = $"[{e.TraceMessage.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{e.TraceMessage.Level}] [{e.TraceMessage.Source}] [{e.TraceMessage.ThreadId}] [{e.TraceMessage.Message}]{Environment.NewLine}";
                if (e.TraceMessage.Exception != null)
                {
                    text += $"{e.TraceMessage.Exception}{Environment.NewLine}";
                }

                Trace.Text += text;
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

            var options = new MqttClientOptions { ClientId = ClientId.Text };

            if (UseTcp.IsChecked == true)
            {
                options.ChannelOptions = new MqttClientTcpOptions
                {
                    Server = Server.Text,
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

            try
            {
                if (_mqttClient != null)
                {
                    await _mqttClient.DisconnectAsync();
                }

                var factory = new MqttFactory();
                _mqttClient = factory.CreateMqttClient();
                await _mqttClient.ConnectAsync(options);
            }
            catch (Exception exception)
            {
                Trace.Text += exception + Environment.NewLine;
            }
        }

        private async void Publish(object sender, RoutedEventArgs e)
        {
            if (_mqttClient == null)
            {
                return;
            }

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
                if (Text.IsChecked == true)
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

                await _mqttClient.PublishAsync(message);
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
                await _mqttClient.DisconnectAsync();
            }
            catch (Exception exception)
            {
                Trace.Text += exception + Environment.NewLine;
            }
        }

        private void Clear(object sender, RoutedEventArgs e)
        {
            Trace.Text = string.Empty;
        }

        private async void Subscribe(object sender, RoutedEventArgs e)
        {
            if (_mqttClient == null)
            {
                return;
            }

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

                await _mqttClient.SubscribeAsync(new TopicFilter(SubscribeTopic.Text, qos));
            }
            catch (Exception exception)
            {
                Trace.Text += exception + Environment.NewLine;
            }
        }

        private async void Unsubscribe(object sender, RoutedEventArgs e)
        {
            if (_mqttClient == null)
            {
                return;
            }

            try
            {
                await _mqttClient.UnsubscribeAsync(SubscribeTopic.Text);
            }
            catch (Exception exception)
            {
                Trace.Text += exception + Environment.NewLine;
            }
        }

        // This code is for the Wiki at GitHub!
        // ReSharper disable once UnusedMember.Local
        private async Task WikiCode()
        {
            {
                // Create a new MQTT client
                var services = new ServiceCollection()
                    .AddMqttClient()
                    .BuildServiceProvider();

                var factory = new MqttFactory(services);
                var client = factory.CreateMqttClient();

                {
                    // Create TCP based options using the builder
                    var options = new MqttClientOptionsBuilder()
                        .WithClientId("Client1")
                        .WithTcpServer("broker.hivemq.com")
                        .WithCredentials("bud", "%spencer%")
                        .WithTls()
                        .WithCleanSession()
                        .Build();

                    await client.ConnectAsync(options);
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
            }

            // ----------------------------------
            {
                var services = new ServiceCollection()
                    .AddMqttServer(options =>
                    {
                        options.ConnectionValidator = c =>
                        {
                            if (c.ClientId.Length < 10)
                            {
                                return MqttConnectReturnCode.ConnectionRefusedIdentifierRejected;
                            }

                            if (c.Username != "mySecretUser")
                            {
                                return MqttConnectReturnCode.ConnectionRefusedBadUsernameOrPassword;
                            }

                            if (c.Password != "mySecretPassword")
                            {
                                return MqttConnectReturnCode.ConnectionRefusedBadUsernameOrPassword;
                            }

                            return MqttConnectReturnCode.ConnectionAccepted;
                        };
                    })
                    .BuildServiceProvider();

                var factory = new MqttFactory(services);
                var mqttServer = factory.CreateMqttServer();
                await mqttServer.StartAsync();

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

        }

        private async void StartServer(object sender, RoutedEventArgs e)
        {
            if (_mqttServer != null)
            {
                return;
            }

            _mqttServer = new MqttFactory().CreateMqttServer(o =>
            {
                o.DefaultEndpointOptions.Port = int.Parse(ServerPort.Text);
            });

            await _mqttServer.StartAsync();
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
    }
}
