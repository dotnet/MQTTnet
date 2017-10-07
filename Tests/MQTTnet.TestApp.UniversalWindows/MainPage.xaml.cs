using System;
using System.Text;
using Windows.UI.Core;
using Windows.UI.Xaml;
using MQTTnet.Core;
using MQTTnet.Core.Client;
using MQTTnet.Core.Diagnostics;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Protocol;

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
                var text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{e.Level}] [{e.Source}] [{e.ThreadId}] [{e.Message}]{Environment.NewLine}";
                if (e.Exception != null)
                {
                    text += $"{e.Exception}{Environment.NewLine}";
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

            options.UserName = User.Text;
            options.Password = Password.Text;
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

                var factory = new MqttClientFactory();
                _mqttClient = factory.CreateMqttClient(options);
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

                var message = new MqttApplicationMessage(
                    Topic.Text,
                    payload,
                    qos,
                    Retain.IsChecked == true);

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
    }
}
