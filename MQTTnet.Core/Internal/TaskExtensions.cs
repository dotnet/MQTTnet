using System;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Core.Exceptions;

namespace MQTTnet.Core.Internal
{
    public static class TaskExtensions
    {
        public static async Task TimeoutAfter(this Task task, TimeSpan timeout)
        {
            var timeoutTask = Task.Delay(timeout);
            var finishedTask = await Task.WhenAny(timeoutTask, task).ConfigureAwait(false);

            if (finishedTask == timeoutTask || task.IsCanceled)
            {
                throw new MqttCommunicationTimedOutException();
            }

            if (task.IsCanceled)
            {
                throw new TaskCanceledException();
            }

            if (task.IsFaulted)
            {
                throw new MqttCommunicationException(task.Exception);
            }

            ////return TimeoutAfter(task.ContinueWith(t => 0), timeout);
        }

        public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout)
        {
            var timeoutTask = Task.Delay(timeout);
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
                throw new MqttCommunicationException(task.Exception);
            }

            return task.Result;

            ////            using (var cancellationTokenSource = new CancellationTokenSource())
            ////            {
            ////                var tcs = new TaskCompletionSource<TResult>();

            ////                cancellationTokenSource.Token.Register(() =>
            ////                {
            ////                    tcs.TrySetCanceled();
            ////                });

            ////                try
            ////                {
            ////#pragma warning disable 4014
            ////                    task.ContinueWith(t =>
            ////#pragma warning restore 4014
            ////                    {
            ////                        if (t.IsFaulted)
            ////                        {
            ////                            tcs.TrySetException(t.Exception);
            ////                        }

            ////                        if (t.IsCompleted)
            ////                        {
            ////                            tcs.TrySetResult(t.Result);
            ////                        }

            ////                        return t.Result;
            ////                    }, cancellationTokenSource.Token).ConfigureAwait(false);

            ////                    cancellationTokenSource.CancelAfter(timeout);
            ////                    return await tcs.Task.ConfigureAwait(false);
            ////                }
            ////                catch (TaskCanceledException)
            ////                {
            ////                    throw new MqttCommunicationTimedOutException();
            ////                }
            ////                catch (Exception e)
            ////                {
            ////                    throw new MqttCommunicationException(e);
            ////                }
            ////            }
        }
    }
}
