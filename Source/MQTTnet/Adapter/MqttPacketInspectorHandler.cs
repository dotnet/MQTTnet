// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using MQTTnet.Diagnostics;
using MQTTnet.Formatter;
using MQTTnet.Internal;

namespace MQTTnet.Adapter
{
    public sealed class MqttPacketInspectorHandler : IMqttPacketInspectorHandler
    {
        readonly MemoryStream _receivedPacketBuffer = new MemoryStream();
        readonly MqttNetSourceLogger _logger;
        readonly AsyncEvent<InspectMqttPacketEventArgs> _asyncEvent;
        
        public MqttPacketInspectorHandler(AsyncEvent<InspectMqttPacketEventArgs> asyncEvent, IMqttNetLogger logger)
        {
            _asyncEvent = asyncEvent ?? throw new ArgumentNullException(nameof(asyncEvent));
            
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            _logger = logger.WithSource(nameof(MqttPacketInspectorHandler));
        }

        public void BeginReceivePacket()
        {
            _receivedPacketBuffer?.SetLength(0);
        }

        public void EndReceivePacket()
        {
            if (!_asyncEvent.HasHandlers)
            {
                return;
            }

            var buffer = _receivedPacketBuffer.ToArray();
            _receivedPacketBuffer.SetLength(0);

            InspectPacket(buffer, MqttPacketFlowDirection.Inbound);
        }

        public void BeginSendPacket(MqttPacketBuffer buffer)
        {
            if (!_asyncEvent.HasHandlers)
            {
                return;
            }

            // Create a copy of the actual packet so that the inspector gets no access
            // to the internal buffers. This is waste of memory but this feature is only
            // intended for debugging etc. so that this is OK.
            var bufferCopy = buffer.ToArray();

            InspectPacket(bufferCopy.ToArray(), MqttPacketFlowDirection.Outbound);
        }

        public void FillReceiveBuffer(byte[] buffer)
        {
            if (!_asyncEvent.HasHandlers)
            {
                return;
            }
            
            _receivedPacketBuffer?.Write(buffer, 0, buffer.Length);
        }

        void InspectPacket(byte[] buffer, MqttPacketFlowDirection direction)
        {
            try
            {
                var eventArgs = new InspectMqttPacketEventArgs
                {
                    Buffer = buffer,
                    Direction = direction
                };

                _asyncEvent.InvokeAsync(eventArgs).GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Error while inspecting packet.");
            }
        }
    }
}