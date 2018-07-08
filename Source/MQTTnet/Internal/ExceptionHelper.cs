using MQTTnet.Exceptions;

namespace MQTTnet.Internal
{
    public static class ExceptionHelper
    {
        public static void ThrowGracefulSocketClose()
        {
            throw new MqttCommunicationClosedGracefullyException();
        }

        public static void ThrowIfGracefulSocketClose(int readBytesCount)
        {
            if (readBytesCount <= 0)
            {
                throw new MqttCommunicationClosedGracefullyException();
            }
        }
    }
}
