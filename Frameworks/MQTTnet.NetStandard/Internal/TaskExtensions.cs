using System;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Exceptions;

namespace MQTTnet.Internal
{
    public static class TaskExtensions
    {
        public static async Task TimeoutAfter(this Task task, TimeSpan timeout)
        {
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                try
                {
                    var timeoutTask = Task.Delay(timeout, cancellationTokenSource.Token);
                    var finishedTask = await Task.WhenAny(timeoutTask, task).ConfigureAwait(false);

                    if (finishedTask == timeoutTask)
                    {
                        throw new MqttCommunicationTimedOutException();
                    }

                    if (task.IsCanceled)
                    {
                        throw new TaskCanceledException();
                    }

                    if (task.IsFaulted)
                    {
                        throw new MqttCommunicationException(task.Exception.GetBaseException());
                    }
                }
                finally
                {
                    cancellationTokenSource.Cancel();
                }
            }
        }

        public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout)
        {
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                try
                {
                    var timeoutTask = Task.Delay(timeout, cancellationTokenSource.Token);
                    var finishedTask = await Task.WhenAny(timeoutTask, task).ConfigureAwait(false);

                    if (finishedTask == timeoutTask)
                    {
                        throw new MqttCommunicationTimedOutException();
                    }

                    if (task.IsCanceled)
                    {
                        throw new TaskCanceledException();
                    }

                    if (task.IsFaulted)
                    {
                        throw new MqttCommunicationException(task.Exception.GetBaseException());
                    }

                    return task.Result;
                }
                finally
                {
                    cancellationTokenSource.Cancel();
                }
            }
        }
    }
}
