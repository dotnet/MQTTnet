﻿using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MQTTnet.Server;
using Newtonsoft.Json;

namespace MQTTnet.TestApp.UniversalWindows
{
    public sealed class JsonServerStorage : IMqttServerStorage
    {
        readonly string _filename = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "Retained.json");

        public Task SaveRetainedMessagesAsync(IList<MqttApplicationMessage> messages)
        {
            var json = JsonConvert.SerializeObject(messages);
            File.WriteAllText(_filename, json);

            return Task.CompletedTask;
        }

        public async Task<IList<MqttApplicationMessage>> LoadRetainedMessagesAsync()
        {
            await Task.CompletedTask;

            if (!File.Exists(_filename))
            {
                return new List<MqttApplicationMessage>();
            }

            try
            {
                var json = File.ReadAllText(_filename);
                return JsonConvert.DeserializeObject<List<MqttApplicationMessage>>(json);
            }
            catch
            {
                return new List<MqttApplicationMessage>();
            }
        }

        public void Clear()
        {
            if (File.Exists(_filename))
            {
                File.Delete(_filename);
            }
        }
    }
}
