// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Adapter;
using MQTTnet.Server;
using System;

namespace MQTTnet.AspNetCore
{
    sealed class PacketFragmentationFeature(Func<IMqttChannelAdapter, bool> allowPacketFragmentationSelector)
    {
        public Func<IMqttChannelAdapter, bool> AllowPacketFragmentationSelector { get; } = allowPacketFragmentationSelector;

        public static bool CanAllowPacketFragmentation(IMqttChannelAdapter channelAdapter, MqttServerTcpEndpointBaseOptions? endpointOptions)
        {
            //if (endpointOptions != null && endpointOptions.AllowPacketFragmentationSelector != null)
            //{
            //    return endpointOptions.AllowPacketFragmentationSelector(channelAdapter);
            //}

            // In the AspNetCore environment, we need to exclude WebSocket before AllowPacketFragmentation.
            if (channelAdapter.IsWebSocketConnection() == true)
            {
                return false;
            }

            return endpointOptions == null || endpointOptions.AllowPacketFragmentation;
        }
    }
}
