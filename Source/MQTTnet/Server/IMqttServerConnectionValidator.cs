using System.Threading.Tasks;

namespace MQTTnet.Server
{
    public interface IMqttServerConnectionValidator
    {
        Task ValidateConnection(MqttConnectionValidatorContext context);
    }
}
