// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using MQTTnet.Adapter;
using System;

namespace MQTTnet.AspNetCore
{
    public static class MqttChannelAdapterExtensions
    {
        public static bool? IsWebSocketConnection(this IMqttChannelAdapter channelAdapter)
        {
            ArgumentNullException.ThrowIfNull(channelAdapter);
            return channelAdapter is IAspNetCoreMqttChannelAdapter adapter
                ? adapter.Features != null && adapter.Features.Get<WebSocketConnectionFeature>() != null
                : null;
        }

        /// <summary>
        /// Retrieves the requested feature from the feature collection of channelAdapter.
        /// </summary>
        /// <typeparam name="TFeature"></typeparam>
        /// <param name="channelAdapter"></param>
        /// <returns></returns>
        public static TFeature? GetFeature<TFeature>(this IMqttChannelAdapter channelAdapter)
        {
            ArgumentNullException.ThrowIfNull(channelAdapter);
            return channelAdapter is IAspNetCoreMqttChannelAdapter adapter && adapter.Features != null
                ? adapter.Features.Get<TFeature>()
                : default;
        }

        /// <summary>
        /// When the channelAdapter is a WebSocket connection, it can get an associated <see cref="HttpContext"/>.
        /// </summary>
        /// <param name="channelAdapter"></param>
        /// <returns></returns>
        public static HttpContext? GetHttpContext(this IMqttChannelAdapter channelAdapter)
        {
            ArgumentNullException.ThrowIfNull(channelAdapter);
            return channelAdapter is IAspNetCoreMqttChannelAdapter adapter
                ? adapter.HttpContext
                : null;
        }
    }
}
