namespace MQTTnet.Client
{
    public class MqttPacketIdentifierProvider
    {
        private readonly object _syncRoot = new object();
        private ushort _value;

        public void Reset()
        {
            lock (_syncRoot)
            {
                _value = 0;
            }
        }

        public ushort GetNextPacketIdentifier()
        {
            lock (_syncRoot)
            {
                _value++;

                if (_value == 0)
                {
                    // As per official MQTT documentation the package identifier should never be 0.
                    _value = 1;
                }

                return _value;
            }
        }
    }
}
