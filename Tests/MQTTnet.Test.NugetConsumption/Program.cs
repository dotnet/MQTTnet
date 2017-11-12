using System;

namespace MQTTnet.Test.NugetConsumption
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new MqttFactory().CreateMqttServer();
        }
    }
}
