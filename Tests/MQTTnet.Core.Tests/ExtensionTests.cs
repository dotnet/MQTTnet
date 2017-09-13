using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Core.Exceptions;
using MQTTnet.Core.Internal;

namespace MQTTnet.Core.Tests
{
    [TestClass]
    public class ExtensionTests
    {
        [ExpectedException(typeof( MqttCommunicationTimedOutException ) )]
        [TestMethod]
        public async Task TestTimeoutAfter()
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500)).TimeoutAfter(TimeSpan.FromMilliseconds(100));
        }

        [ExpectedException(typeof( MqttCommunicationTimedOutException))]
        [TestMethod]
        public async Task TestTimeoutAfterWithResult()
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500)).ContinueWith(t =>  5).TimeoutAfter(TimeSpan.FromMilliseconds(100));
        }
        
        [TestMethod]
        public async Task TestTimeoutAfterCompleteInTime()
        {
            var result = await Task.Delay( TimeSpan.FromMilliseconds( 100 ) ).ContinueWith( t => 5 ).TimeoutAfter( TimeSpan.FromMilliseconds( 500 ) );
            Assert.AreEqual( 5, result );
        }
    }
}
