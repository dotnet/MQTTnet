using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using MQTTnet.Adapter;
using MQTTnet.Server;
using System;
using System.Threading.Tasks;
using MQTTnet.Formatter;

namespace MQTTnet.AspNetCore
{
    public class MqttConnectionHandler : ConnectionHandler, IMqttServerAdapter
    {
        public event EventHandler<MqttServerAdapterClientAcceptedEventArgs> ClientAccepted;

        public override async Task OnConnectedAsync(ConnectionContext connection)
        {
            // required for websocket transport to work
            var transferFormatFeature = connection.Features.Get<ITransferFormatFeature>();
            if (transferFormatFeature != null)
            {
                transferFormatFeature.ActiveFormat = TransferFormat.Binary;
            }

            using (var adapter = new MqttConnectionContext(new MqttPacketFormatterAdapter(), connection))
            {
                var args = new MqttServerAdapterClientAcceptedEventArgs(adapter);
                ClientAccepted?.Invoke(this, args);

                await args.SessionTask;
            }
        }

        public Task StartAsync(IMqttServerOptions options)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}
