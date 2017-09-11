using System;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Core.Exceptions;

namespace MQTTnet.Core.Internal
{
    public static class TaskExtensions
    {
        public static Task TimeoutAfter( this Task task, TimeSpan timeout )
        {
            return TimeoutAfter( task.ContinueWith( t => 0 ), timeout );
        }

        public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout)
        {
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var tcs = new TaskCompletionSource<TResult>();

                cancellationTokenSource.Token.Register(() =>
                {
                    tcs.TrySetCanceled();
                } );
                
                try
                {
                    cancellationTokenSource.CancelAfter(timeout);
                    task.ContinueWith( t =>
                    {
                        if (t.IsFaulted)
                        {
                            tcs.TrySetException(t.Exception);
                        }

                        if (t.IsCompleted)
                        {
                            tcs.TrySetResult(t.Result);
                        }
                    }, cancellationTokenSource.Token );

                    return await tcs.Task;
                }
                catch (TaskCanceledException)
                {
                    throw new MqttCommunicationTimedOutException();
                }
                catch (Exception e)
                {
                    throw new MqttCommunicationException(e);
                }
            }
        }
    }
}
