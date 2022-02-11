// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace MQTTnet.Diagnostics
{
    public static class MqttNetSourceLoggerExtensions
    {
        /*
         * The logger uses generic parameters in order to avoid boxing of parameter values like integers etc.
         */

        public static MqttNetSourceLogger WithSource(this IMqttNetLogger logger, string source)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            return new MqttNetSourceLogger(logger, source);
        }

        public static void Verbose<TParameter1>(this MqttNetSourceLogger logger, string message, TParameter1 parameter1)
        {
            if (!logger.IsEnabled)
            {
                return;
            }

            logger.Publish(MqttNetLogLevel.Verbose, message, new object[] {parameter1}, null);
        }

        public static void Verbose<TParameter1, TParameter2>(this MqttNetSourceLogger logger, string message, TParameter1 parameter1, TParameter2 parameter2)
        {
            if (!logger.IsEnabled)
            {
                return;
            }

            logger.Publish(MqttNetLogLevel.Verbose, message, new object[] {parameter1, parameter2}, null);
        }

        public static void Verbose<TParameter1, TParameter2, TParameter3>(this MqttNetSourceLogger logger, string message, TParameter1 parameter1, TParameter2 parameter2,
            TParameter3 parameter3)
        {
            if (!logger.IsEnabled)
            {
                return;
            }

            logger.Publish(MqttNetLogLevel.Verbose, message, new object[] {parameter1, parameter2, parameter3}, null);
        }

        public static void Verbose(this MqttNetSourceLogger logger, string message)
        {
            if (!logger.IsEnabled)
            {
                return;
            }

            logger.Publish(MqttNetLogLevel.Verbose, message, null, null);
        }

        public static void Info<TParameter1>(this MqttNetSourceLogger logger, string message, TParameter1 parameter1)
        {
            if (!logger.IsEnabled)
            {
                return;
            }

            logger.Publish(MqttNetLogLevel.Info, message, new object[] {parameter1}, null);
        }
        
        public static void Info<TParameter1, TParameter2>(this MqttNetSourceLogger logger, string message, TParameter1 parameter1, TParameter2 parameter2)
        {
            if (!logger.IsEnabled)
            {
                return;
            }

            logger.Publish(MqttNetLogLevel.Info, message, new object[] {parameter1, parameter2}, null);
        }

        public static void Info(this MqttNetSourceLogger logger, string message)
        {
            if (!logger.IsEnabled)
            {
                return;
            }
            
            logger.Publish(MqttNetLogLevel.Info, message, null, null);
        }

        public static void Warning<TParameter1>(this MqttNetSourceLogger logger, Exception exception, string message, TParameter1 parameter1)
        {
            if (!logger.IsEnabled)
            {
                return;
            }
            
            logger.Publish(MqttNetLogLevel.Warning, message, new object[] {parameter1}, exception);
        }
        
        public static void Warning<TParameter1, TParameter2>(this MqttNetSourceLogger logger, Exception exception, string message, TParameter1 parameter1, TParameter2 parameter2)
        {
            if (!logger.IsEnabled)
            {
                return;
            }
            
            logger.Publish(MqttNetLogLevel.Warning, message, new object[] {parameter1, parameter2}, exception);
        }
        
        public static void Warning(this MqttNetSourceLogger logger, Exception exception, string message)
        {
            if (!logger.IsEnabled)
            {
                return;
            }
            
            logger.Publish(MqttNetLogLevel.Warning, message, null, exception);
        }
        
        public static void Warning<TParameter1>(this MqttNetSourceLogger logger, string message, TParameter1 parameter1)
        {
            if (!logger.IsEnabled)
            {
                return;
            }
            
            logger.Publish(MqttNetLogLevel.Warning, message, new object[] {parameter1}, null);
        }
        
        public static void Warning<TParameter1, TParameter2>(this MqttNetSourceLogger logger, string message, TParameter1 parameter1, TParameter2 parameter2)
        {
            if (!logger.IsEnabled)
            {
                return;
            }
            
            logger.Publish(MqttNetLogLevel.Warning, message, new object[] {parameter1, parameter2}, null);
        }
        
        public static void Warning(this MqttNetSourceLogger logger, string message)
        {
            if (!logger.IsEnabled)
            {
                return;
            }
            
            logger.Publish(MqttNetLogLevel.Warning, message, null, null);
        }

        public static void Error<TParameter1>(this MqttNetSourceLogger logger, Exception exception, string message, TParameter1 parameter1)
        {
            if (!logger.IsEnabled)
            {
                return;
            }
            
            logger.Publish(MqttNetLogLevel.Error, message, new object[] {parameter1}, exception);
        }
        
        public static void Error<TParameter1, TParameter2>(this MqttNetSourceLogger logger, Exception exception, string message, TParameter1 parameter1, TParameter2 parameter2)
        {
            if (!logger.IsEnabled)
            {
                return;
            }
            
            logger.Publish(MqttNetLogLevel.Error, message, new object[] {parameter1, parameter2}, exception);
        }

        public static void Error(this MqttNetSourceLogger logger, Exception exception, string message)
        {
            if (!logger.IsEnabled)
            {
                return;
            }
            
            logger.Publish(MqttNetLogLevel.Error, message, null, exception);
        }

        public static void Error(this MqttNetSourceLogger logger, string message)
        {
            if (!logger.IsEnabled)
            {
                return;
            }
            
            logger.Publish(MqttNetLogLevel.Error, message, null, null);
        }
    }
}