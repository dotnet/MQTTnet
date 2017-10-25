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
using MQTTnet.Core.Packets;
using MQTTnet.Core.Protocol;
using MQTTnet.Core.Server;
using MQTTnet.Implementations;

namespace MQTTnet.TestApp.UniversalWindows
{
    public sealed partial class MainPage
    {
        private IMqttClient _mqttClient;

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
            MqttClientOptions options = null;
            if (UseTcp.IsChecked == true)
            {
                options = new MqttClientTcpOptions
                {
                    Server = Server.Text
                };
            }

            if (UseWs.IsChecked == true)
            {
                options = new MqttClientWebSocketOptions
                {
                    Uri = Server.Text
                };
            }

            if (options == null)
            {
                throw new InvalidOperationException();
            }

            options.Credentials = new MqttClientCredentials
            {
                Username = User.Text,
                Password = Password.Text
            };

            options.ClientId = ClientId.Text;
            options.TlsOptions.UseTls = UseTls.IsChecked == true;
            options.TlsOptions.IgnoreCertificateChainErrors = true;
            options.TlsOptions.IgnoreCertificateRevocationErrors = true;
            options.TlsOptions.AllowUntrustedCertificates = true;

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
            var services = new ServiceCollection()
                .AddMqttClient()
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

            var mqttClient = factory.CreateMqttClient();

            // ----------------------------------

            var tcpOptions = new MqttClientTcpOptions
            {
                Server = "broker.hivemq.org",
                ClientId = "TestClient"
            };

            await mqttClient.ConnectAsync(tcpOptions);

            // ----------------------------------

            var secureTcpOptions = new MqttClientTcpOptions
            {
                Server = "broker.hivemq.org",
                ClientId = "TestClient",
                TlsOptions = new MqttClientTlsOptions
                {
                    UseTls = true,
                    IgnoreCertificateChainErrors = true,
                    IgnoreCertificateRevocationErrors = true,
                    AllowUntrustedCertificates = true
                }
            };

            // ----------------------------------

            var wsOptions = new MqttClientWebSocketOptions
            {
                Uri = "broker.hivemq.com:8000/mqtt",
                ClientId = "TestClient"
            };

            await mqttClient.ConnectAsync(wsOptions);

            // ----------------------------------
            {
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
    }
}
