using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MQTTnet;
using MQTTnet.Server;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Client.Options;
using MQTTnet.Protocol;
using MQTTnet.Client.Connecting;
using MQTTnet.Formatter;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Receiving;

namespace MQTTnet.TestApp.SimpleServer
{
	public partial class Form1 : Form
	{
		private string _port = 1883.ToString();
		private IMqttServer _mqttServer;
		private IManagedMqttClient _managedMqttClientPublisher;
		private IManagedMqttClient _managedMqttClientSubscriber;

		private System.Timers.Timer timer;

		public Form1()
		{
			InitializeComponent();

			timer = new System.Timers.Timer()
			{
				AutoReset = true,
				Enabled = true,
				Interval = 1000
			};
			timer.Elapsed += Timer_Elapsed;
		}

		private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			this.BeginInvoke((System.Windows.Forms.MethodInvoker)delegate ()
			{
				//Server
				TextBoxPort.Enabled = _mqttServer == null;
				ButtonServerStart.Enabled = _mqttServer == null;
				ButtonServerStop.Enabled = _mqttServer != null;

				//Publisher
				ButtonPublisherStart.Enabled = _managedMqttClientPublisher == null;
				ButtonPublisherStop.Enabled = _managedMqttClientPublisher != null;

				//Subscriber
				ButtonSubscriberStart.Enabled = _managedMqttClientSubscriber == null;
				ButtonSubscriberStop.Enabled = _managedMqttClientSubscriber != null;
			});
		}

		#region Server

		private async void ButtonServerStart_Click(object sender, EventArgs e)
		{

			if (_mqttServer != null)
			{
				return;
			}

			JsonServerStorage storage = null;
			bool ServerPersistRetainedMessages_IsChecked = true;
			bool ServerAllowPersistentSessions_IsChecked = true;
			if (ServerPersistRetainedMessages_IsChecked == true)
			{
				storage = new JsonServerStorage();

				if (ServerPersistRetainedMessages_IsChecked == true)
				{
					storage.Clear();
				}
			}

			_mqttServer = new MqttFactory().CreateMqttServer();

			var options = new MqttServerOptions();
			options.DefaultEndpointOptions.Port = int.Parse(TextBoxPort.Text);
			options.Storage = storage;
			options.EnablePersistentSessions = ServerAllowPersistentSessions_IsChecked == true;
			options.ConnectionValidator = new MqttServerConnectionValidatorDelegate(c =>
			{
				if (c.ClientId.Length < 10)
				{
					c.ReturnCode = MqttConnectReturnCode.ConnectionRefusedIdentifierRejected;
					return;
				}

				if (c.Username != "username")
				{
					c.ReturnCode = MqttConnectReturnCode.ConnectionRefusedBadUsernameOrPassword;
					return;
				}

				if (c.Password != "password")
				{
					c.ReturnCode = MqttConnectReturnCode.ConnectionRefusedBadUsernameOrPassword;
					return;
				}

				c.ReturnCode = MqttConnectReturnCode.ConnectionAccepted;
			});
			

			try
			{
				await _mqttServer.StartAsync(options);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error Occurs", MessageBoxButtons.OK, MessageBoxIcon.Error);
				await _mqttServer.StopAsync();
				_mqttServer = null;
			}
		}

		private async void ButtonServerStop_Click(object sender, EventArgs e)
		{
			if (_mqttServer == null)
			{
				return;
			}

			await _mqttServer.StopAsync();
			_mqttServer = null;
		}

		private void TextBoxPort_TextChanged(object sender, EventArgs e)
		{
			if (int.TryParse(TextBoxPort.Text, out int p))
			{
				_port = TextBoxPort.Text.Trim();
			}
			else
			{
				TextBoxPort.Text = _port;
				TextBoxPort.SelectionStart = TextBoxPort.Text.Length;
				TextBoxPort.SelectionLength = 0;
			}
		}

		#endregion

		#region Publiser

		private async void ButtonPublisherStart_Click(object sender, EventArgs e)
		{
			/*
			var options = new ManagedMqttClientOptionsBuilder()
							.WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
							.WithClientOptions(
								new MqttClientOptionsBuilder()
									.WithClientId("ClientPublisher")
									.WithTcpServer("localhost", int.Parse(TextBoxPort.Text))
									.WithTls()
									.Build()
								)
							.Build();

			_managedMqttClientPublisher = new MqttFactory().CreateManagedMqttClient();
			//await _managedMqttClientPublisher.SubscribeAsync(new TopicFilterBuilder().WithTopic(TextBoxTopic.Text.Trim()).Build());

			_managedMqttClientPublisher.UseConnectedHandler(HandleClientPublisherConnected);
			await _managedMqttClientPublisher.StartAsync(options);
			*/


			var mqttFactory = new MqttFactory();

			var tlsOptions = new MqttClientTlsOptions
			{
				UseTls = false,
				IgnoreCertificateChainErrors = true,
				IgnoreCertificateRevocationErrors = true,
				AllowUntrustedCertificates = true
			};

			var options = new MqttClientOptions
			{
				ClientId = "ClientPublisher",
				ProtocolVersion = MqttProtocolVersion.V311
			};

			//use tcp
			options.ChannelOptions = new MqttClientTcpOptions
			{
				Server = "localhost",
				Port = int.Parse(TextBoxPort.Text.Trim()),
				TlsOptions = tlsOptions
			};

			bool useWs = false;
			if (useWs)
			{
				options.ChannelOptions = new MqttClientWebSocketOptions
				{
					Uri = "localhost",
					TlsOptions = tlsOptions
				};
			}

			/*
			if (UseWs4Net.IsChecked == true)
			{
				options.ChannelOptions = new MqttClientWebSocketOptions
				{
					Uri = Server.Text,
					TlsOptions = tlsOptions
				};

				mqttFactory.UseWebSocket4Net();
			}
			*/

			if (options.ChannelOptions == null)
			{
				throw new InvalidOperationException();
			}

			//credentials
			options.Credentials = new MqttClientCredentials
			{
				/*
				Username = User.Text,
				Password = Encoding.UTF8.GetBytes(Password.Text)
				*/

				Username = "username",
				Password = Encoding.UTF8.GetBytes("password")
			};

			options.CleanSession = true;
			options.KeepAlivePeriod = TimeSpan.FromSeconds(5);

			/*
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
			*/

			_managedMqttClientPublisher = mqttFactory.CreateManagedMqttClient();
			_managedMqttClientPublisher.UseApplicationMessageReceivedHandler(HandleReceivedApplicationMessage);
			_managedMqttClientPublisher.ConnectedHandler = new MqttClientConnectedHandlerDelegate(x => OnPublisherConnected(x));
			_managedMqttClientPublisher.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(x => OnPublisherDisconnected(x));

			await _managedMqttClientPublisher.StartAsync(new ManagedMqttClientOptions
			{
				ClientOptions = options
			});


			//try
			//{

			//}
			//catch (Exception exception)
			//{
			//	Trace.Text += exception + Environment.NewLine;
			//}
		}

		private void OnPublisherConnected(MqttClientConnectedEventArgs x)
		{
			MessageBox.Show("Publisher Connected", "ConnectHandler", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void OnPublisherDisconnected(MqttClientDisconnectedEventArgs x)
		{
			MessageBox.Show("Publisher Disonnected", "ConnectHandler", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		/*
		private void HandleClientPublisherConnected(MqttClientConnectedEventArgs arg)
		{
			var item = $"ConnectedTimestamp: {DateTime.Now:O}";

			this.BeginInvoke((System.Windows.Forms.MethodInvoker)delegate ()
			{
				TextBoxSubscriber.Text = item + Environment.NewLine + TextBoxSubscriber.Text;
			});
		}
		*/

		private async void ButtonPublisherStop_Click(object sender, EventArgs e)
		{
			if (_managedMqttClientPublisher == null)
			{
				return;
			}

			await _managedMqttClientPublisher.StopAsync();
			_managedMqttClientPublisher = null;
		}

		private async void ButtonPublish_Click(object sender, EventArgs e)
		{
			((Button)sender).Enabled = false;
			try
			{
				MqttQualityOfServiceLevel qos;
				//qos = MqttQualityOfServiceLevel.AtMostOnce;
				qos = MqttQualityOfServiceLevel.AtLeastOnce;
				//qos = MqttQualityOfServiceLevel.ExactlyOnce;


				var payload = new byte[0];
				payload = Encoding.UTF8.GetBytes(TextBoxPublish.Text);
				//payload = Convert.FromBase64String(TextBoxPublish.Text);

				var message = new MqttApplicationMessageBuilder()
					//.WithContentType(ContentType.Text)
					//.WithResponseTopic(ResponseTopic.Text)
					.WithTopic(TextBoxTopicPublished.Text.Trim())
					.WithPayload(payload)
					.WithQualityOfServiceLevel(qos)
					.WithRetainFlag(true)
					.Build();

				if (_managedMqttClientPublisher != null)
				{
					await _managedMqttClientPublisher.PublishAsync(message);
				}
				message = null;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error Occurs", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			((Button)sender).Enabled = true;
		}

		private void ButtonGeneratePublishedMessage_Click(object sender, EventArgs e)
		{
			string dtString = DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString();
			string message = String.Format("{{\"dt\":\"{0}\"}}", dtString);

			TextBoxPublish.Text = message;
		}



		#endregion

		#region Subscriber

		private async void ButtonSubscriberStart_Click(object sender, EventArgs e)
		{
			/*
			var options = new ManagedMqttClientOptionsBuilder()
							.WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
							.WithClientOptions(
								new MqttClientOptionsBuilder()
									.WithClientId("ClientSubscriber")
									.WithTcpServer("localhost", int.Parse(TextBoxPort.Text))
									//.WithTls()
									.Build()
								)
							.Build();

			_managedMqttClientSubscriber = new MqttFactory().CreateManagedMqttClient();
			_managedMqttClientSubscriber.UseApplicationMessageReceivedHandler(HandleReceivedApplicationMessage);
			_managedMqttClientSubscriber.ConnectedHandler = new MqttClientConnectedHandlerDelegate(x => OnSubscriberConnected(x));
			_managedMqttClientSubscriber.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(x => OnSubscriberDisconnected(x));

			await _managedMqttClientSubscriber.SubscribeAsync(new TopicFilterBuilder().WithTopic(TextBoxTopic.Text.Trim()).Build());
			await _managedMqttClientSubscriber.StartAsync(options);
			*/

			var mqttFactory = new MqttFactory();

			var tlsOptions = new MqttClientTlsOptions
			{
				UseTls = false,
				IgnoreCertificateChainErrors = true,
				IgnoreCertificateRevocationErrors = true,
				AllowUntrustedCertificates = true
			};

			var options = new MqttClientOptions
			{
				ClientId = "ClientSubscriber",
				ProtocolVersion = MqttProtocolVersion.V311
			};

			//use tcp
			options.ChannelOptions = new MqttClientTcpOptions
			{
				Server = "localhost",
				Port = int.Parse(TextBoxPort.Text.Trim()),
				TlsOptions = tlsOptions
			};

			bool useWs = false;
			if (useWs)
			{
				options.ChannelOptions = new MqttClientWebSocketOptions
				{
					Uri = "localhost",
					TlsOptions = tlsOptions
				};
			}

			/*
			if (UseWs4Net.IsChecked == true)
			{
				options.ChannelOptions = new MqttClientWebSocketOptions
				{
					Uri = Server.Text,
					TlsOptions = tlsOptions
				};

				mqttFactory.UseWebSocket4Net();
			}
			*/

			if (options.ChannelOptions == null)
			{
				throw new InvalidOperationException();
			}

			//credentials
			/*
			options.Credentials = new MqttClientCredentials
			{
				Username = User.Text,
				Password = Encoding.UTF8.GetBytes(Password.Text)
			};
			*/

			options.CleanSession = true;
			options.KeepAlivePeriod = TimeSpan.FromSeconds(5);


			/*
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
			*/

			_managedMqttClientSubscriber = mqttFactory.CreateManagedMqttClient();
			//_managedMqttClientSubscriber.UseApplicationMessageReceivedHandler(HandleReceivedApplicationMessage);
			_managedMqttClientSubscriber.ConnectedHandler = new MqttClientConnectedHandlerDelegate(x => OnSubscriberConnected(x));
			_managedMqttClientSubscriber.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(x => OnSubscriberDisconnected(x));
			_managedMqttClientSubscriber.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(x => OnSubscriberMessageReceived(x));

			await _managedMqttClientSubscriber.StartAsync(new ManagedMqttClientOptions
			{
				ClientOptions = options
			});


			//try
			//{

			//}
			//catch (Exception exception)
			//{
			//	Trace.Text += exception + Environment.NewLine;
			//}
		}

		private void OnSubscriberMessageReceived(MqttApplicationMessageReceivedEventArgs x)
		{
			//MessageBox.Show("OK", "ConnectHandler", MessageBoxButtons.OK, MessageBoxIcon.Information);


			var item = $"Timestamp: {DateTime.Now:O} | Topic: {x.ApplicationMessage.Topic} | Payload: {x.ApplicationMessage.ConvertPayloadToString()} | QoS: {x.ApplicationMessage.QualityOfServiceLevel}";

			this.BeginInvoke((System.Windows.Forms.MethodInvoker)delegate ()
			{
				TextBoxSubscriber.Text = item + Environment.NewLine + TextBoxSubscriber.Text;
			});

		}

		private async void ButtonSubscriberStop_Click(object sender, EventArgs e)
		{
			if (_managedMqttClientSubscriber == null)
			{
				return;
			}

			await _managedMqttClientSubscriber.StopAsync();
			_managedMqttClientSubscriber = null;
		}

		private async void ButtonSubscribe_Click(object sender, EventArgs e)
		{
			await _managedMqttClientSubscriber.SubscribeAsync(new TopicFilterBuilder().WithTopic(TextBoxTopicSubscribed.Text.Trim()).Build());
			MessageBox.Show("Topic " + TextBoxTopicSubscribed.Text.Trim() + " is subscribed", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private async void HandleReceivedApplicationMessage(MqttApplicationMessageReceivedEventArgs eventArgs)
		{
			var item = $"Timestamp: {DateTime.Now:O} | Topic: {eventArgs.ApplicationMessage.Topic} | Payload: {eventArgs.ApplicationMessage.ConvertPayloadToString()} | QoS: {eventArgs.ApplicationMessage.QualityOfServiceLevel}";

			this.BeginInvoke((System.Windows.Forms.MethodInvoker)delegate ()
			{
				TextBoxSubscriber.Text = item + Environment.NewLine + TextBoxSubscriber.Text;
			});
		}



		private void OnSubscriberConnected(MqttClientConnectedEventArgs x)
		{
			MessageBox.Show("Subscriber Connected", "ConnectHandler", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void OnSubscriberDisconnected(MqttClientDisconnectedEventArgs x)
		{
			MessageBox.Show("Subscriber Disonnected", "ConnectHandler", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}



		#endregion

	}
}
