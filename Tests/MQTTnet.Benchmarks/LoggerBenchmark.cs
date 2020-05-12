﻿using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using MQTTnet.Diagnostics;

namespace MQTTnet.Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net461)]
    [RPlotExporter]
    [MemoryDiagnoser]
    public class LoggerBenchmark
    {
        IMqttNetLogger _logger;
        IMqttNetScopedLogger _childLogger;
        bool _useHandler;

        [GlobalSetup]
        public void Setup()
        {
            _logger = new MqttNetLogger();
            _childLogger = _logger.CreateScopedLogger("child");

            MqttNetGlobalLogger.LogMessagePublished += OnLogMessagePublished;
        }

        private void OnLogMessagePublished(object sender, MqttNetLogMessagePublishedEventArgs eventArgs)
        {
            if (_useHandler)
            {
                eventArgs.LogMessage.ToString();
            }
        }

        [Benchmark]
        public void Log_10000_Messages_NoHandler()
        {
            _useHandler = false;
            for (var i = 0; i < 10000; i++)
            {
                _childLogger.Verbose("test log message {0}", "parameter");
            }
        }

        [Benchmark]
        public void Log_10000_Messages_WithHandler()
        {
            _useHandler = true;
            for (var i = 0; i < 10000; i++)
            {
                _childLogger.Verbose("test log message {0}", "parameter");
            }
        }
    }
}
