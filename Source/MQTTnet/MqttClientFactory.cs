// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Implementations;
using MQTTnet.LowLevelClient;

namespace MQTTnet;

public sealed class MqttClientFactory
{
    IMqttClientAdapterFactory _clientAdapterFactory;

    public MqttClientFactory() : this(new MqttNetNullLogger())
    {
    }

    public MqttClientFactory(IMqttNetLogger logger)
    {
        DefaultLogger = logger ?? throw new ArgumentNullException(nameof(logger));

        _clientAdapterFactory = new MqttClientAdapterFactory();
    }

    public IMqttNetLogger DefaultLogger { get; }

    public IDictionary<object, object> Properties { get; } = new Dictionary<object, object>();

    public MqttApplicationMessageBuilder CreateApplicationMessageBuilder()
    {
        return new MqttApplicationMessageBuilder();
    }

    public MqttClientDisconnectOptionsBuilder CreateClientDisconnectOptionsBuilder()
    {
        return new MqttClientDisconnectOptionsBuilder();
    }

    public MqttClientOptionsBuilder CreateClientOptionsBuilder()
    {
        return new MqttClientOptionsBuilder();
    }

    public ILowLevelMqttClient CreateLowLevelMqttClient()
    {
        return CreateLowLevelMqttClient(DefaultLogger);
    }

    public ILowLevelMqttClient CreateLowLevelMqttClient(IMqttNetLogger logger)
    {
        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        return new LowLevelMqttClient(_clientAdapterFactory, logger);
    }

    public ILowLevelMqttClient CreateLowLevelMqttClient(IMqttClientAdapterFactory clientAdapterFactory)
    {
        if (clientAdapterFactory == null)
        {
            throw new ArgumentNullException(nameof(clientAdapterFactory));
        }

        return new LowLevelMqttClient(_clientAdapterFactory, DefaultLogger);
    }

    public ILowLevelMqttClient CreateLowLevelMqttClient(IMqttNetLogger logger, IMqttClientAdapterFactory clientAdapterFactory)
    {
        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        if (clientAdapterFactory == null)
        {
            throw new ArgumentNullException(nameof(clientAdapterFactory));
        }

        return new LowLevelMqttClient(_clientAdapterFactory, logger);
    }

    public IMqttClient CreateMqttClient()
    {
        return CreateMqttClient(DefaultLogger);
    }

    public IMqttClient CreateMqttClient(IMqttNetLogger logger)
    {
        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        return new MqttClient(_clientAdapterFactory, logger);
    }

    public IMqttClient CreateMqttClient(IMqttClientAdapterFactory clientAdapterFactory)
    {
        if (clientAdapterFactory == null)
        {
            throw new ArgumentNullException(nameof(clientAdapterFactory));
        }

        return new MqttClient(clientAdapterFactory, DefaultLogger);
    }

    public IMqttClient CreateMqttClient(IMqttNetLogger logger, IMqttClientAdapterFactory clientAdapterFactory)
    {
        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        if (clientAdapterFactory == null)
        {
            throw new ArgumentNullException(nameof(clientAdapterFactory));
        }

        return new MqttClient(clientAdapterFactory, logger);
    }

    public MqttClientSubscribeOptionsBuilder CreateSubscribeOptionsBuilder()
    {
        return new MqttClientSubscribeOptionsBuilder();
    }

    public MqttTopicFilterBuilder CreateTopicFilterBuilder()
    {
        return new MqttTopicFilterBuilder();
    }

    public MqttClientUnsubscribeOptionsBuilder CreateUnsubscribeOptionsBuilder()
    {
        return new MqttClientUnsubscribeOptionsBuilder();
    }

    public MqttClientFactory UseClientAdapterFactory(IMqttClientAdapterFactory clientAdapterFactory)
    {
        _clientAdapterFactory = clientAdapterFactory ?? throw new ArgumentNullException(nameof(clientAdapterFactory));
        return this;
    }
}