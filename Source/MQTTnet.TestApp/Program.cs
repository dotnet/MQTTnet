// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Diagnostics;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.TestApp
{
    public static class Program
    {
        public static void Main()
        {
            Console.WriteLine($"MQTTnet - TestApp.{TargetFrameworkProvider.TargetFramework}");
            Console.WriteLine("1 = Start client");
            Console.WriteLine("2 = Start server");
            Console.WriteLine("3 = Start performance test");
            Console.WriteLine("4 = Start managed client");
            Console.WriteLine("5 = Start public broker test");
            Console.WriteLine("6 = Start server & client");
            Console.WriteLine("7 = Client flow test");
            Console.WriteLine("8 = Start performance test (client only)");
            Console.WriteLine("9 = Start server (no trace)");
            Console.WriteLine("a = Start QoS 2 benchmark");
            Console.WriteLine("b = Start QoS 1 benchmark");
            Console.WriteLine("c = Start QoS 0 benchmark");
            Console.WriteLine("d = Start server with logging");
            Console.WriteLine("e = Start Message Throughput Test");
            Console.WriteLine("f = Start AsyncLock Test");

            var pressedKey = Console.ReadKey(true);
            if (pressedKey.KeyChar == '1')
            {
                Task.Run(ClientTest.RunAsync);
            }
            else if (pressedKey.KeyChar == '2')
            {
                Task.Run(ServerTest.RunAsync);
            }
            else if (pressedKey.KeyChar == '3')
            {
                Task.Run(PerformanceTest.RunClientAndServer);
            }
            else if (pressedKey.KeyChar == '4')
            {
                Task.Run(ManagedClientTest.RunAsync);
            }
            else if (pressedKey.KeyChar == '5')
            {
                Task.Run(PublicBrokerTest.RunAsync);
            }
            else if (pressedKey.KeyChar == '6')
            {
                Task.Run(ServerAndClientTest.RunAsync);
            }
            else if (pressedKey.KeyChar == '7')
            {
                Task.Run(ClientFlowTest.RunAsync);
            }
            else if (pressedKey.KeyChar == '8')
            {
                PerformanceTest.RunClientOnly();
                return;
            }
            else if (pressedKey.KeyChar == '9')
            {
                ServerTest.RunEmptyServer();
                return;
            }
            else if (pressedKey.KeyChar == 'a')
            {
                Task.Run(PerformanceTest.RunQoS2Test);
            }
            else if (pressedKey.KeyChar == 'b')
            {
                Task.Run(PerformanceTest.RunQoS1Test);
            }
            else if (pressedKey.KeyChar == 'c')
            {
                Task.Run(PerformanceTest.RunQoS0Test);
            }
            else if (pressedKey.KeyChar == 'd')
            {
                Task.Run(ServerTest.RunEmptyServerWithLogging);
            }
            else if (pressedKey.KeyChar == 'e')
            {
                Task.Run(new MessageThroughputTest().Run);
            }
            else if (pressedKey.KeyChar == 'f')
            {
                Task.Run(new AsyncLockTest().Run);
            }

            Thread.Sleep(Timeout.Infinite);
        }
    }
}