using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Server;

namespace MQTTnet.Core.Tests
{
    [TestClass]
    public class TopicFilterComparerTests
    {
        [TestMethod]
        public void TopicFilterComparer_DirectMatch()
        {
            CompareAndAssert("A/B/C", "A/B/C", true);
        }

        [TestMethod]
        public void TopicFilterComparer_DirectNoMatch()
        {
            CompareAndAssert("A/B/X", "A/B/C", false);
        }

        [TestMethod]
        public void TopicFilterComparer_MiddleOneLevelWildcardMatch()
        {
            CompareAndAssert("A/B/C", "A/+/C", true);
        }

        [TestMethod]
        public void TopicFilterComparer_MiddleOneLevelWildcardNoMatch()
        {
            CompareAndAssert("A/B/C/D", "A/+/C", false);
        }

        [TestMethod]
        public void TopicFilterComparer_BeginningOneLevelWildcardMatch()
        {
            CompareAndAssert("A/B/C", "+/B/C", true);
        }

        [TestMethod]
        public void TopicFilterComparer_EndOneLevelWildcardMatch()
        {
            CompareAndAssert("A/B/C", "A/B/+", true);
        }

        [TestMethod]
        public void TopicFilterComparer_EndMultipleLevelsWildcardMatch()
        {
            CompareAndAssert("A/B/C", "A/#", true);
        }

        [TestMethod]
        public void TopicFilterComparer_EndMultipleLevelsWildcardNoMatch()
        {
            CompareAndAssert("A/B/C/D", "A/C/#", false);
        }

        [TestMethod]
        public void TopicFilterComparer_AllLevelsWildcardMatch()
        {
            CompareAndAssert("A/B/C/D", "#", true);
        }

        private void CompareAndAssert(string topic, string filter, bool expectedResult)
        {
            Assert.AreEqual(expectedResult, MqttTopicFilterComparer.IsMatch(topic, filter));
        }
    }
}
