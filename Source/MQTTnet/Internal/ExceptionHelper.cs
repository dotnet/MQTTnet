using MQTTnet.Exceptions;

namespace MQTTnet.Internal
{
    public static class ExceptionHelper
    {
        public static void ThrowGracefulSocketClose()
        {
            throw new MqttCommunicationClosedGracefullyException();
        }
    }
}
