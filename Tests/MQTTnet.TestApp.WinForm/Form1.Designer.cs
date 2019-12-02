namespace MQTTnet.TestApp.SimpleServer
{
	partial class Form1
	{
		/// <summary>
		///  Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		///  Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		///  Required method for Designer support - do not modify
		///  the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.label1 = new System.Windows.Forms.Label();
			this.TextBoxPort = new System.Windows.Forms.TextBox();
			this.ButtonServerStart = new System.Windows.Forms.Button();
			this.ButtonServerStop = new System.Windows.Forms.Button();
			this.label2 = new System.Windows.Forms.Label();
			this.maskedTextBox1 = new System.Windows.Forms.MaskedTextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.TextBoxTopicPublished = new System.Windows.Forms.TextBox();
			this.label4 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.ButtonPublisherStop = new System.Windows.Forms.Button();
			this.ButtonPublisherStart = new System.Windows.Forms.Button();
			this.TextBoxPublish = new System.Windows.Forms.TextBox();
			this.ButtonPublish = new System.Windows.Forms.Button();
			this.ButtonSubscriberStop = new System.Windows.Forms.Button();
			this.ButtonSubscriberStart = new System.Windows.Forms.Button();
			this.TextBoxSubscriber = new System.Windows.Forms.TextBox();
			this.ButtonGeneratePublishedMessage = new System.Windows.Forms.Button();
			this.label6 = new System.Windows.Forms.Label();
			this.TextBoxTopicSubscribed = new System.Windows.Forms.TextBox();
			this.ButtonSubscribe = new System.Windows.Forms.Button();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(119, 24);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(32, 15);
			this.label1.TabIndex = 0;
			this.label1.Text = "Port:";
			// 
			// TextBoxPort
			// 
			this.TextBoxPort.Location = new System.Drawing.Point(172, 21);
			this.TextBoxPort.Name = "TextBoxPort";
			this.TextBoxPort.Size = new System.Drawing.Size(100, 23);
			this.TextBoxPort.TabIndex = 1;
			this.TextBoxPort.Text = "1883";
			this.TextBoxPort.TextChanged += new System.EventHandler(this.TextBoxPort_TextChanged);
			// 
			// ButtonServerStart
			// 
			this.ButtonServerStart.Location = new System.Drawing.Point(433, 21);
			this.ButtonServerStart.Name = "ButtonServerStart";
			this.ButtonServerStart.Size = new System.Drawing.Size(75, 23);
			this.ButtonServerStart.TabIndex = 2;
			this.ButtonServerStart.Text = "Start";
			this.ButtonServerStart.UseVisualStyleBackColor = true;
			this.ButtonServerStart.Click += new System.EventHandler(this.ButtonServerStart_Click);
			// 
			// ButtonServerStop
			// 
			this.ButtonServerStop.Location = new System.Drawing.Point(514, 21);
			this.ButtonServerStop.Name = "ButtonServerStop";
			this.ButtonServerStop.Size = new System.Drawing.Size(75, 23);
			this.ButtonServerStop.TabIndex = 3;
			this.ButtonServerStop.Text = "Stop";
			this.ButtonServerStop.UseVisualStyleBackColor = true;
			this.ButtonServerStop.Click += new System.EventHandler(this.ButtonServerStop_Click);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(33, 25);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(39, 15);
			this.label2.TabIndex = 4;
			this.label2.Text = "Server";
			// 
			// maskedTextBox1
			// 
			this.maskedTextBox1.Location = new System.Drawing.Point(0, 0);
			this.maskedTextBox1.Name = "maskedTextBox1";
			this.maskedTextBox1.Size = new System.Drawing.Size(100, 23);
			this.maskedTextBox1.TabIndex = 5;
			this.maskedTextBox1.Text = "maskedTextBox1";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(33, 79);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(90, 15);
			this.label3.TabIndex = 5;
			this.label3.Text = "Topic Published";
			// 
			// TextBoxTopicPublished
			// 
			this.TextBoxTopicPublished.Location = new System.Drawing.Point(164, 76);
			this.TextBoxTopicPublished.Name = "TextBoxTopicPublished";
			this.TextBoxTopicPublished.Size = new System.Drawing.Size(425, 23);
			this.TextBoxTopicPublished.TabIndex = 1;
			this.TextBoxTopicPublished.Text = "brand/type/group/code";
			this.TextBoxTopicPublished.TextChanged += new System.EventHandler(this.TextBoxPort_TextChanged);
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(33, 109);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(90, 15);
			this.label4.TabIndex = 5;
			this.label4.Text = "Client Publisher";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(33, 224);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(96, 15);
			this.label5.TabIndex = 5;
			this.label5.Text = "Client Subscriber";
			// 
			// ButtonPublisherStop
			// 
			this.ButtonPublisherStop.Location = new System.Drawing.Point(514, 105);
			this.ButtonPublisherStop.Name = "ButtonPublisherStop";
			this.ButtonPublisherStop.Size = new System.Drawing.Size(75, 23);
			this.ButtonPublisherStop.TabIndex = 3;
			this.ButtonPublisherStop.Text = "Stop";
			this.ButtonPublisherStop.UseVisualStyleBackColor = true;
			this.ButtonPublisherStop.Click += new System.EventHandler(this.ButtonPublisherStop_Click);
			// 
			// ButtonPublisherStart
			// 
			this.ButtonPublisherStart.Location = new System.Drawing.Point(433, 105);
			this.ButtonPublisherStart.Name = "ButtonPublisherStart";
			this.ButtonPublisherStart.Size = new System.Drawing.Size(75, 23);
			this.ButtonPublisherStart.TabIndex = 2;
			this.ButtonPublisherStart.Text = "Start";
			this.ButtonPublisherStart.UseVisualStyleBackColor = true;
			this.ButtonPublisherStart.Click += new System.EventHandler(this.ButtonPublisherStart_Click);
			// 
			// TextBoxPublish
			// 
			this.TextBoxPublish.Location = new System.Drawing.Point(33, 134);
			this.TextBoxPublish.Name = "TextBoxPublish";
			this.TextBoxPublish.Size = new System.Drawing.Size(394, 23);
			this.TextBoxPublish.TabIndex = 1;
			this.TextBoxPublish.TextChanged += new System.EventHandler(this.TextBoxPort_TextChanged);
			// 
			// ButtonPublish
			// 
			this.ButtonPublish.Location = new System.Drawing.Point(514, 134);
			this.ButtonPublish.Name = "ButtonPublish";
			this.ButtonPublish.Size = new System.Drawing.Size(75, 23);
			this.ButtonPublish.TabIndex = 3;
			this.ButtonPublish.Text = "Publish";
			this.ButtonPublish.UseVisualStyleBackColor = true;
			this.ButtonPublish.Click += new System.EventHandler(this.ButtonPublish_Click);
			// 
			// ButtonSubscriberStop
			// 
			this.ButtonSubscriberStop.Location = new System.Drawing.Point(514, 220);
			this.ButtonSubscriberStop.Name = "ButtonSubscriberStop";
			this.ButtonSubscriberStop.Size = new System.Drawing.Size(75, 23);
			this.ButtonSubscriberStop.TabIndex = 3;
			this.ButtonSubscriberStop.Text = "Stop";
			this.ButtonSubscriberStop.UseVisualStyleBackColor = true;
			this.ButtonSubscriberStop.Click += new System.EventHandler(this.ButtonSubscriberStop_Click);
			// 
			// ButtonSubscriberStart
			// 
			this.ButtonSubscriberStart.Location = new System.Drawing.Point(433, 220);
			this.ButtonSubscriberStart.Name = "ButtonSubscriberStart";
			this.ButtonSubscriberStart.Size = new System.Drawing.Size(75, 23);
			this.ButtonSubscriberStart.TabIndex = 2;
			this.ButtonSubscriberStart.Text = "Start";
			this.ButtonSubscriberStart.UseVisualStyleBackColor = true;
			this.ButtonSubscriberStart.Click += new System.EventHandler(this.ButtonSubscriberStart_Click);
			// 
			// TextBoxSubscriber
			// 
			this.TextBoxSubscriber.Location = new System.Drawing.Point(33, 309);
			this.TextBoxSubscriber.Multiline = true;
			this.TextBoxSubscriber.Name = "TextBoxSubscriber";
			this.TextBoxSubscriber.Size = new System.Drawing.Size(556, 182);
			this.TextBoxSubscriber.TabIndex = 6;
			// 
			// ButtonGeneratePublishedMessage
			// 
			this.ButtonGeneratePublishedMessage.Location = new System.Drawing.Point(433, 133);
			this.ButtonGeneratePublishedMessage.Name = "ButtonGeneratePublishedMessage";
			this.ButtonGeneratePublishedMessage.Size = new System.Drawing.Size(75, 23);
			this.ButtonGeneratePublishedMessage.TabIndex = 2;
			this.ButtonGeneratePublishedMessage.Text = "Random";
			this.ButtonGeneratePublishedMessage.UseVisualStyleBackColor = true;
			this.ButtonGeneratePublishedMessage.Click += new System.EventHandler(this.ButtonGeneratePublishedMessage_Click);
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(33, 252);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(96, 15);
			this.label6.TabIndex = 5;
			this.label6.Text = "Topic Subscribed";
			// 
			// TextBoxTopicSubscribed
			// 
			this.TextBoxTopicSubscribed.Location = new System.Drawing.Point(164, 249);
			this.TextBoxTopicSubscribed.Name = "TextBoxTopicSubscribed";
			this.TextBoxTopicSubscribed.Size = new System.Drawing.Size(344, 23);
			this.TextBoxTopicSubscribed.TabIndex = 1;
			this.TextBoxTopicSubscribed.Text = "brand/type/group/code";
			this.TextBoxTopicSubscribed.TextChanged += new System.EventHandler(this.TextBoxPort_TextChanged);
			// 
			// ButtonSubscribe
			// 
			this.ButtonSubscribe.Location = new System.Drawing.Point(514, 248);
			this.ButtonSubscribe.Name = "ButtonSubscribe";
			this.ButtonSubscribe.Size = new System.Drawing.Size(75, 23);
			this.ButtonSubscribe.TabIndex = 3;
			this.ButtonSubscribe.Text = "Subscribe";
			this.ButtonSubscribe.UseVisualStyleBackColor = true;
			this.ButtonSubscribe.Click += new System.EventHandler(this.ButtonSubscribe_Click);
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(627, 530);
			this.Controls.Add(this.TextBoxSubscriber);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.ButtonServerStop);
			this.Controls.Add(this.ButtonServerStart);
			this.Controls.Add(this.TextBoxPort);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.TextBoxTopicPublished);
			this.Controls.Add(this.ButtonPublisherStop);
			this.Controls.Add(this.ButtonPublisherStart);
			this.Controls.Add(this.TextBoxPublish);
			this.Controls.Add(this.ButtonPublish);
			this.Controls.Add(this.ButtonSubscriberStop);
			this.Controls.Add(this.ButtonSubscriberStart);
			this.Controls.Add(this.ButtonGeneratePublishedMessage);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.TextBoxTopicSubscribed);
			this.Controls.Add(this.ButtonSubscribe);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.Name = "Form1";
			this.Text = "MQTT Testing";

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox TextBoxPort;
		private System.Windows.Forms.Button ButtonServerStart;
		private System.Windows.Forms.Button ButtonServerStop;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.MaskedTextBox maskedTextBox1;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Button ButtonPublisherStop;
		private System.Windows.Forms.Button ButtonPublisherStart;
		private System.Windows.Forms.TextBox TextBoxPublish;
		private System.Windows.Forms.Button ButtonPublish;
		private System.Windows.Forms.Button ButtonSubscriberStop;
		private System.Windows.Forms.Button ButtonSubscriberStart;
		private System.Windows.Forms.TextBox TextBoxSubscriber;
		private System.Windows.Forms.Button ButtonGeneratePublishedMessage;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.TextBox TextBoxTopicPublished;
		private System.Windows.Forms.Button ButtonSubscribe;
		private System.Windows.Forms.TextBox TextBoxTopicSubscribed;
	}
}

