using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Core.ManagedClient
{
    public class ManagedMqttClientStorageManager
    {
        private readonly List<MqttApplicationMessage> _applicationMessages = new List<MqttApplicationMessage>();
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private IManagedMqttClientStorage _storage;

        public async Task SetStorageAsync(IManagedMqttClientStorage storage)
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                _storage = storage;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task AddAsync(MqttApplicationMessage applicationMessage)
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_storage == null)
                {
                    return;
                }

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
                if (_storage == null)
                {
                    return;
                }

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
