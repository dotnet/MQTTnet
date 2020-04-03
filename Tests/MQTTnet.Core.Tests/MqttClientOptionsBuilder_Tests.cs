using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client.Options;
using MQTTnet.Extensions;

namespace MQTTnet.Tests
{
    [TestClass]
    public class MqttClientOptionsBuilder_Tests
    {
        [TestMethod]
        public void WithConnectionUri_Credential_Test()
        {
            var options = new MqttClientOptionsBuilder()
                .WithConnectionUri("mqtt://user:password@127.0.0.1")
                .Build();
            Assert.AreEqual("user", options.Credentials.Username);
            Assert.IsTrue(Encoding.UTF8.GetBytes("password").SequenceEqual(options.Credentials.Password));
        }
    }
}
