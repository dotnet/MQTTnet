using System;

namespace MQTTnet.Core.Diagnostics
{
    public sealed class MqttNetTrace : IMqttNetTraceHandler
    {
        private readonly IMqttNetTraceHandler _traceHandler;

        public MqttNetTrace(IMqttNetTraceHandler traceHandler = null)
        {
            _traceHandler = traceHandler ?? this;
        }
        
        public static event EventHandler<MqttNetTraceMessagePublishedEventArgs> TraceMessagePublished;

        public bool IsEnabled => TraceMessagePublished != null;

        public void Verbose(string source, string message, params object[] parameters)
        {
            Publish(source, MqttNetTraceLevel.Verbose, null, message, parameters);
        }

        public void Information(string source, string message, params object[] parameters)
        {
            Publish(source, MqttNetTraceLevel.Information, null, message, parameters);
        }

        public void Warning(string source, string message, params object[] parameters)
        {
            Publish(source, MqttNetTraceLevel.Warning, null, message, parameters);
        }

        public void Warning(string source, Exception exception, string message, params object[] parameters)
        {
            Publish(source, MqttNetTraceLevel.Warning, exception, message, parameters);
        }

        public void Error(string source, string message, params object[] parameters)
        {
            Publish(source, MqttNetTraceLevel.Error, null, message, parameters);
        }

        public void Error(string source, Exception exception, string message, params object[] parameters)
        {
            Publish(source, MqttNetTraceLevel.Error, exception, message, parameters);
        }

        public void HandleTraceMessage(MqttNetTraceMessage mqttNetTraceMessage)
        {
            TraceMessagePublished?.Invoke(this, new MqttNetTraceMessagePublishedEventArgs(mqttNetTraceMessage));
        }

        private void Publish(string source, MqttNetTraceLevel traceLevel, Exception exception, string message, params object[] parameters)
        {
            if (!_traceHandler.IsEnabled)
            {
                return;
            }

            var now = DateTime.Now;
            if (parameters?.Length > 0)
            {
                try
                {
                    message = string.Format(message, parameters);
                }
                catch (Exception formatException)
                {
                    Error(nameof(MqttNetTrace), formatException, "Error while tracing message: " + message);
                    return;
                }
            }

            _traceHandler.HandleTraceMessage(new MqttNetTraceMessage(now, Environment.CurrentManagedThreadId, source, traceLevel, message, exception));
        }
    }
}
