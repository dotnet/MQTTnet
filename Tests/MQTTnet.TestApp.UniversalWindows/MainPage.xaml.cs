using System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using MQTTnet.Core.Client;
using MQTTnet.Core.Diagnostics;

namespace MQTTnet.TestApp.UniversalWindows
{
    public sealed partial class MainPage
    {
        private MqttClient _mqttClient;

        public MainPage()
        {
            InitializeComponent();

            MqttTrace.TraceMessagePublished += OnTraceMessagePublished;
        }

        private async void OnTraceMessagePublished(object sender, MqttTraceMessagePublishedEventArgs e)
        {
            await Trace.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                var text = $"[{DateTime.Now:O}] [{e.Level}] [{e.Source}] [{e.ThreadId}] [{e.Message}]{Environment.NewLine}";
                if (e.Exception != null)
                {
                    text += $"{e.Exception}{Environment.NewLine}";
                }

                Trace.Text += text;
            });
        }

        private async void Connect(object sender, RoutedEventArgs e)
        {
            var options = new MqttClientOptions
            {
                Server = Server.Text,
                UserName = User.Text,
                Password = Password.Text,
                ClientId = ClientId.Text
            };

            options.SslOptions.UseSsl = UseSsl.IsChecked == true;

            try
            {
                if (_mqttClient != null)
                {
                    await _mqttClient.DisconnectAsync();
                }

                var factory = new MqttClientFactory();
                _mqttClient = factory.CreateMqttClient(options);
                await _mqttClient.ConnectAsync();
            }
            catch (Exception exception)
            {
                Trace.Text += exception + Environment.NewLine;
            }
        }
    }
}
