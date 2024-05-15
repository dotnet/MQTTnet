// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Diagnostics;
using MQTTnet.Server.Internal.Adapter;

namespace MQTTnet.Server;

public sealed class MqttServerFactory
{
    public MqttServerFactory() : this(new MqttNetNullLogger())
    {
    }

    public MqttServerFactory(IMqttNetLogger logger)
    {
        DefaultLogger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IMqttNetLogger DefaultLogger { get; }

    public IList<Func<MqttServerFactory, IMqttServerAdapter>> DefaultServerAdapters { get; } = new List<Func<MqttServerFactory, IMqttServerAdapter>>
    {
        factory => new MqttTcpServerAdapter()
    };

    public IDictionary<object, object> Properties { get; } = new Dictionary<object, object>();

    public MqttApplicationMessageBuilder CreateApplicationMessageBuilder()
    {
        return new MqttApplicationMessageBuilder();
    }

    public MqttServer CreateMqttServer(MqttServerOptions options)
    {
        return CreateMqttServer(options, DefaultLogger);
    }

    public MqttServer CreateMqttServer(MqttServerOptions options, IMqttNetLogger logger)
    {
        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        var serverAdapters = DefaultServerAdapters.Select(a => a.Invoke(this));
        return CreateMqttServer(options, serverAdapters, logger);
    }

    public MqttServer CreateMqttServer(MqttServerOptions options, IEnumerable<IMqttServerAdapter> serverAdapters, IMqttNetLogger logger)
    {
        if (serverAdapters == null)
        {
            throw new ArgumentNullException(nameof(serverAdapters));
        }

        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        return new MqttServer(options, serverAdapters, logger);
    }

    public MqttServer CreateMqttServer(MqttServerOptions options, IEnumerable<IMqttServerAdapter> serverAdapters)
    {
        if (serverAdapters == null)
        {
            throw new ArgumentNullException(nameof(serverAdapters));
        }

        return new MqttServer(options, serverAdapters, DefaultLogger);
    }

    public MqttServerClientDisconnectOptionsBuilder CreateMqttServerClientDisconnectOptionsBuilder()
    {
        return new MqttServerClientDisconnectOptionsBuilder();
    }


    public MqttServerStopOptionsBuilder CreateMqttServerStopOptionsBuilder()
    {
        return new MqttServerStopOptionsBuilder();
    }

    public MqttServerOptionsBuilder CreateServerOptionsBuilder()
    {
        return new MqttServerOptionsBuilder();
    }
}