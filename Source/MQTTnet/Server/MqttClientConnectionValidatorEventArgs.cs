using System;
using System.Threading.Tasks;

namespace MQTTnet.Server
{
 
    public class MqttClientConnectionValidatorEventArgs : EventArgs
    {
        public MqttClientConnectionValidatorEventArgs(MqttConnectionValidatorContext context)
        {
            Context = context;
        }
        public MqttConnectionValidatorContext Context { get; set; }
    }
}
