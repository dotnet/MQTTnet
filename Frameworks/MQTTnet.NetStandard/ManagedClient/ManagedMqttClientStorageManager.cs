using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.ManagedClient
{
    public class ManagedMqttClientStorageManager
    {
        private readonly List<MqttApplicationMessage> _applicationMessages = new List<MqttApplicationMessage>();
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly IManagedMqttClientStorage _storage;

        public ManagedMqttClientStorageManager(IManagedMqttClientStorage storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public async Task LoadQueuedMessagesAsync()
        {
            var loadedMessages = await _storage.LoadQueuedMessagesAsync().ConfigureAwait(false);
            foreach (var loadedMessage in loadedMessages)
            {
                _applicationMessages.Add(loadedMessage);
            }
        }

        public async Task AddAsync(MqttApplicationMessage applicationMessage)
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                _applicationMessages.Add(applicationMessage);
                await SaveAsync().ConfigureAwait(false);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task RemoveAsync(MqttApplicationMessage applicationMessage)
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                var index = _applicationMessages.IndexOf(applicationMessage);
                if (index == -1)
                {
                    return;
                }

                _applicationMessages.RemoveAt(index);
                await SaveAsync().ConfigureAwait(false);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private Task SaveAsync()
        {
            return _storage.SaveQueuedMessagesAsync(_applicationMessages);
        }
    }
}
