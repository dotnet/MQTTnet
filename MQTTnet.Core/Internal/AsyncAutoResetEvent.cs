using System.Collections.Generic;
using System.Threading.Tasks;

namespace MQTTnet.Core.Internal
{
    public sealed class AsyncGate
    {
        private readonly Queue<TaskCompletionSource<bool>> _waitingTasks = new Queue<TaskCompletionSource<bool>>();

        public Task WaitOneAsync()
        {
            var tcs = new TaskCompletionSource<bool>();
            lock (_waitingTasks)
            {
                _waitingTasks.Enqueue(tcs);
            }

            return tcs.Task;
        }

        public void Set()
        {
            lock (_waitingTasks)
            {
                if (_waitingTasks.Count > 0)
                {
                    _waitingTasks.Dequeue().SetResult(true);
                }
            }
        }
    }
}
