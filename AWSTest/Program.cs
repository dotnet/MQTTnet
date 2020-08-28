using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Client.Subscribing;
using MQTTnet.Formatter;
using MQTTnet.Protocol;

namespace AWSTest
{
	class Program
	{
		static async Task Main(string[] args)
		{
			var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
			store.Open(OpenFlags.ReadWrite);
			var thumbprint = @"b3a132f6db350ec01f557191abbb4436c9192460";
			var cert = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false)[0];

// Create a new MQTT client.
			var factory = new MqttFactory();
			var mqttClient = factory.CreateMqttClient();

			MqttClientOptionsBuilderTlsParameters tlsParameters = new MqttClientOptionsBuilderTlsParameters
			{
				UseTls = true,
				Certificates = new [] {cert},
				ApplicationProtocols = new List<SslApplicationProtocol> {new SslApplicationProtocol("x-amzn-mqtt-ca")}
			};

// Create TCP based options using the builder.
			var connectOptions = new MqttClientOptionsBuilder()
				.WithClientId("test-1")
				.WithCleanSession(false)
				.WithTcpServer("a11ycnpbdonxr-ats.iot.eu-central-1.amazonaws.com", 443)
				.WithProtocolVersion(MqttProtocolVersion.V311)
				.WithTls(tlsParameters)
				.Build();

			var conResult = await mqttClient.ConnectAsync(connectOptions, CancellationToken.None);
			Console.Out.WriteLine("Is session present: " + conResult.IsSessionPresent);
			
			Console.Out.WriteLine(conResult);
			mqttClient.UseApplicationMessageReceivedHandler(Handler);
			var result = await mqttClient.SubscribeAsync(new MqttClientSubscribeOptions {TopicFilters = new List<MqttTopicFilter> {new MqttTopicFilter {QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce, Topic = "topic_1"}}}, CancellationToken.None);

			Console.ReadLine();

		}

		private static Task Handler(MqttApplicationMessageReceivedEventArgs arg)
		{
			var message = Encoding.UTF8.GetString(arg.ApplicationMessage.Payload);
			Console.Out.WriteLine(message);
			return Task.CompletedTask;
		}
	}
}
