using System;
using System.IO;
using System.Linq;
using MQTTnet.Diagnostics;
using MQTTnet.Diagnostics.PacketInspection;

namespace MQTTnet.Adapter
{
    public sealed class MqttPacketInspectorHandler
    {
        readonly MemoryStream _receivedPacketBuffer;
        readonly IMqttPacketInspector _packetInspector;
        readonly IMqttNetScopedLogger _logger;

        public MqttPacketInspectorHandler(IMqttPacketInspector packetInspector, IMqttNetLogger logger)
        {
            _packetInspector = packetInspector;

            if (packetInspector != null)
            {
                _receivedPacketBuffer = new MemoryStream();
            }

            if (logger == null) throw new ArgumentNullException(nameof(logger));
            _logger = logger.CreateScopedLogger(nameof(MqttPacketInspectorHandler));
        }

        public void BeginReceivePacket()
        {
            _receivedPacketBuffer?.SetLength(0);
        }

        public void EndReceivePacket()
        {
            if (_packetInspector == null)
            {
                return;
            }

            var buffer = _receivedPacketBuffer.ToArray();
            _receivedPacketBuffer.SetLength(0);

            InspectPacket(buffer, MqttPacketFlowDirection.Inbound);
        }

        public void BeginSendPacket(ArraySegment<byte> buffer)
        {
            if (_packetInspector == null)
            {
                return;
            }

            // Create a copy of the actual packet so that the inspector gets no access
            // to the internal buffers. This is waste of memory but this feature is only
            // intended for debugging etc. so that this is OK.
            var bufferCopy = buffer.ToArray();

            InspectPacket(bufferCopy, MqttPacketFlowDirection.Outbound);
        }

        public void FillReceiveBuffer(byte[] buffer)
        {
            _receivedPacketBuffer?.Write(buffer, 0, buffer.Length);
        }

        void InspectPacket(byte[] buffer, MqttPacketFlowDirection direction)
        {
            try
            {
                var context = new ProcessMqttPacketContext
                {
                    Buffer = buffer,
                    Direction = direction
                };

                _packetInspector.ProcessMqttPacket(context);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Error while inspecting packet.");
            }
        }
    }
}