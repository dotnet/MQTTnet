// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Exceptions;

namespace MQTTnet.TestApp;

public sealed class AsyncLockTest
{
    public static async Task Run()
    {
        try
        {
            var semaphore = new SemaphoreSlim(1, 1);

            await semaphore.WaitAsync();
            try
            {
                // Wait for data from socket etc...
                // Then get an exception.
                semaphore.Dispose();
                throw new MqttCommunicationException("Connection closed");
            }
            finally
            {
                semaphore.Release();
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception.ToString());
        }












        // var asyncLock = new AsyncLock();
        //
        // using var cancellationToken = new CancellationTokenSource();
        // for (var i = 0; i < 100000; i++)
        // {
        //     using (await asyncLock.EnterAsync(cancellationToken.Token).ConfigureAwait(false))
        //     {
        //     }
        // }
    }
}