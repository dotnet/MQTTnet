using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Diagnostics;
using MQTTnet.Extensions.ManagedClient;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Tests
{
    [TestClass]
    public class MqttFactory_Tests
    {
        [TestMethod]
        public async Task Create_Managed_Client_With_Logger()
        {
            var factory = new MqttFactory();

            // This test compares
            // 1. correct logID
            var logId = "logId";
            var hasInvalidLogId = false;

            // 2. if the total log calls are the same for global and local
            //var globalLogCount = 0;
            var localLogCount = 0;

            var logger = new MqttNetEventLogger(logId);

            // TODO: This is commented out because it is affected by other tests.
            //// we have a theoretical bug here if a concurrent test is also logging
            //var globalLog = new EventHandler<MqttNetLogMessagePublishedEventArgs>((s, e) =>
            //{
            //    if (e.TraceMessage.LogId != logId)
            //    {
            //        invalidLogId = e.TraceMessage.LogId;
            //    }

            //    Interlocked.Increment(ref globalLogCount);
            //});

            //MqttNetGlobalLogger.LogMessagePublished += globalLog;

            logger.LogMessagePublished += (s, e) =>
            {
                if (e.LogMessage.LogId != logId)
                {
                    hasInvalidLogId = true;
                }

                Interlocked.Increment(ref localLogCount);
            };

            var managedClient = factory.CreateManagedMqttClient(logger);
            try
            {
                var clientOptions = new ManagedMqttClientOptionsBuilder();

                clientOptions.WithClientOptions(o => o.WithTcpServer("this_is_an_invalid_host").WithCommunicationTimeout(TimeSpan.FromSeconds(1)));

                // try connect to get some log entries
                await managedClient.StartAsync(clientOptions.Build());

                // wait at least connect timeout or we have some log messages
                var tcs = new TaskCompletionSource<object>();
                managedClient.ConnectingFailedHandler = new ConnectingFailedHandlerDelegate(e => tcs.TrySetResult(null));
                await Task.WhenAny(Task.Delay(managedClient.Options.ClientOptions.CommunicationTimeout), tcs.Task);
            }
            finally
            {
                await managedClient.StopAsync();

                //MqttNetGlobalLogger.LogMessagePublished -= globalLog;
            }

            await Task.Delay(500);

            Assert.IsFalse(hasInvalidLogId);
            Assert.AreNotEqual(0, localLogCount);
        }
    }
}