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

public class MqttClientFactory
{
    readonly IMqttNetLogger _logger;
    IMqttClientAdapterFactory _clientAdapterFactory;

    public IMqttNetLogger DefaultLogger => _logger;

    public IDictionary<object, object> Properties { get; } = new Dictionary<object, object>();

    public MqttClientFactory()
        : this(new MqttNetNullLogger())
    {
    }

    public MqttClientFactory(IMqttNetLogger logger)
        : this(logger, new MqttClientAdapterFactory())
    {
    }

    public MqttClientFactory(IMqttNetLogger logger, IMqttClientAdapterFactory clientAdapterFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _clientAdapterFactory = clientAdapterFactory ?? throw new ArgumentNullException(nameof(clientAdapterFactory));
    }


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
        return new LowLevelMqttClient(_clientAdapterFactory, _logger);
    }

    public ILowLevelMqttClient CreateLowLevelMqttClient(IMqttNetLogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        return new LowLevelMqttClient(_clientAdapterFactory, logger);
    }

    public ILowLevelMqttClient CreateLowLevelMqttClient(IMqttClientAdapterFactory clientAdapterFactory)
    {
        ArgumentNullException.ThrowIfNull(clientAdapterFactory);

        return new LowLevelMqttClient(clientAdapterFactory, _logger);
    }

    public ILowLevelMqttClient CreateLowLevelMqttClient(IMqttNetLogger logger, IMqttClientAdapterFactory clientAdapterFactory)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(clientAdapterFactory);

        return new LowLevelMqttClient(clientAdapterFactory, logger);
    }

    public IMqttClient CreateMqttClient()
    {
        return new MqttClient(_clientAdapterFactory, _logger);
    }

    public IMqttClient CreateMqttClient(IMqttNetLogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        return new MqttClient(_clientAdapterFactory, logger);
    }

    public IMqttClient CreateMqttClient(IMqttClientAdapterFactory clientAdapterFactory)
    {
        ArgumentNullException.ThrowIfNull(clientAdapterFactory);

        return new MqttClient(clientAdapterFactory, _logger);
    }

    public IMqttClient CreateMqttClient(IMqttNetLogger logger, IMqttClientAdapterFactory clientAdapterFactory)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(clientAdapterFactory);

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
        ArgumentNullException.ThrowIfNull(clientAdapterFactory);
        _clientAdapterFactory = clientAdapterFactory;
        return this;
    }
}