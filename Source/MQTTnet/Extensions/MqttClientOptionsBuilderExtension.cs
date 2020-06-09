using System;
using System.Linq;
using MQTTnet.Client.Options;

namespace MQTTnet.Extensions
{
    public static class MqttClientOptionsBuilderExtension
    {
        public static MqttClientOptionsBuilder WithConnectionUri(this MqttClientOptionsBuilder builder, Uri uri)
        {
            var port = uri.IsDefaultPort ? null : (int?) uri.Port;
            switch (uri.Scheme.ToLower())
            {
                case "tcp":
                case "mqtt":
                    builder.WithTcpServer(uri.Host, port);
                    break;

                case "mqtts":
                    builder.WithTcpServer(uri.Host, port).WithTls();
                    break;

                case "ws":
                case "wss":
                    builder.WithWebSocketServer(uri.ToString());
                    break;

                default:
                    throw new ArgumentException("Unexpected scheme in uri.");
            }
            
            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                var userInfo = uri.UserInfo.Split(':');
                var username = userInfo[0];
                var password = userInfo.Length > 1 ? userInfo[1] : "";
                builder.WithCredentials(username, password);
            }

            return builder;
        }

        public static MqttClientOptionsBuilder WithConnectionUri(this MqttClientOptionsBuilder builder, string uri)
        {
            return WithConnectionUri(builder, new Uri(uri, UriKind.Absolute));
        }
    }
}
