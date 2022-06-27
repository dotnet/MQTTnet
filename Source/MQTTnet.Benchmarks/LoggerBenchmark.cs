// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using MQTTnet.Diagnostics;

namespace MQTTnet.Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net60)]
    [RPlotExporter]
    [MemoryDiagnoser]
    public class LoggerBenchmark
    {
        MqttNetNullLogger _nullLogger;
        MqttNetSourceLogger _sourceNullLogger;
        
        MqttNetEventLogger _eventLogger;
        MqttNetSourceLogger _sourceEventLogger;
        
        MqttNetEventLogger _eventLoggerNoListener;
        MqttNetSourceLogger _sourceEventLoggerNoListener;
        
        bool _useHandler;

        [GlobalSetup]
        public void Setup()
        {
            _nullLogger = new MqttNetNullLogger();
            _sourceNullLogger = _nullLogger.WithSource("Source");
            
            _eventLogger = new MqttNetEventLogger();
            _eventLogger.LogMessagePublished += OnLogMessagePublished;
            _sourceEventLogger = _eventLogger.WithSource("Source");
            
            _eventLoggerNoListener = new MqttNetEventLogger();
            _sourceEventLoggerNoListener = _eventLoggerNoListener.WithSource("Source");
        }

        void OnLogMessagePublished(object sender, MqttNetLogMessagePublishedEventArgs eventArgs)
        {
            if (_useHandler)
            {
                eventArgs.LogMessage.ToString();
            }
        }

        [Benchmark]
        public void Log_10000_Messages_No_Listener()
        {
            _useHandler = false;
            
            for (var i = 0; i < 10000; i++)
            {
                _sourceEventLoggerNoListener.Verbose("test log message {0}", "parameter");
            }
        }

        [Benchmark]
        public void Log_10000_Messages_With_To_String()
        {
            _useHandler = true;
            
            for (var i = 0; i < 10000; i++)
            {
                _sourceEventLogger.Verbose("test log message {0}", "parameter");
            }
        }
        
        [Benchmark]
        public void Log_10000_Messages_Without_To_String()
        {
            _useHandler = false;
            
            for (var i = 0; i < 10000; i++)
            {
                _sourceEventLogger.Verbose("test log message {0}", "parameter");
            }
        }
        
        [Benchmark]
        public void Log_10000_Messages_With_NullLogger()
        {
            for (var i = 0; i < 10000; i++)
            {
                _sourceNullLogger.Verbose("test log message {0}", "parameter");
            }
        }
    }
}
