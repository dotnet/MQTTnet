using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Internal;

namespace MQTTnet.ManagedClient
{
    public class ManagedMqttClientStorageManager
    {
        private readonly List<MqttApplicationMessage> _messages = new List<MqttApplicationMessage>();
        private readonly AsyncLock _messagesLock = new AsyncLock();

        private readonly IManagedMqttClientStorage _storage;

        public ManagedMqttClientStorageManager(IManagedMqttClientStorage storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public async Task<List<MqttApplicationMessage>> LoadQueuedMessagesAsync()
        {
            var loadedMessages = await _storage.LoadQueuedMessagesAsync().ConfigureAwait(false);
            _messages.AddRange(loadedMessages);

            return _messages;
        }

        public async Task AddAsync(MqttApplicationMessage applicationMessage)
        {
            using (await _messagesLock.LockAsync(CancellationToken.None).ConfigureAwait(false))
            {
                _messages.Add(applicationMessage);
                await SaveAsync().ConfigureAwait(false);
            }
        }

        public async Task RemoveAsync(MqttApplicationMessage applicationMessage)
        {
            using (await _messagesLock.LockAsync(CancellationToken.None).ConfigureAwait(false))
            {
                var index = _messages.IndexOf(applicationMessage);
                if (index == -1)
                {
                    return;
                }

                _messages.RemoveAt(index);
                await SaveAsync().ConfigureAwait(false);
            }
        }

        private Task SaveAsync()
        {
            return _storage.SaveQueuedMessagesAsync(_messages);
        }
    }
}
