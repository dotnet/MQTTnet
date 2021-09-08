using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Server;
using MQTTnet.Server.Internal;

namespace MQTTnet.Tests
{
    [TestClass]
    public class TopicFilterComparer_Tests
    {
        [TestMethod]
        public void TopicFilterComparer_Plus_Match_With_Separator_Only()
        {
            //A Topic Name or Topic Filter consisting only of the ‘/’ character is valid
            CompareAndAssert("A", "+", true);
        }

        [TestMethod]
        public void TopicFilterComparer_Hash_Match_With_Separator_Only()
        {
            //A Topic Name or Topic Filter consisting only of the ‘/’ character is valid
            CompareAndAssert("/", "#", true);
        }

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
        public void TopicFilterComparer_EndMultipleLevelsWildcardMatchEmptyLevel()
        {
            CompareAndAssert("A/", "A/#", true);
        }

        [TestMethod]
        public void TopicFilterComparer_AllLevelsWildcardMatch()
        {
            CompareAndAssert("A/B/C/D", "#", true);
        }

        [TestMethod]
        public void TopicFilterComparer_MultiLevel_Sport()
        {
            // Tests from official MQTT spec (4.7.1.2 Multi-level wildcard)
            CompareAndAssert("sport/tennis/player1", "sport/tennis/player1/#", true);
            CompareAndAssert("sport/tennis/player1/ranking", "sport/tennis/player1/#", true);
            CompareAndAssert("sport/tennis/player1/score/wimbledon", "sport/tennis/player1/#", true);

            CompareAndAssert("sport/tennis/player1", "sport/tennis/+", true);
            CompareAndAssert("sport/tennis/player2", "sport/tennis/+", true);
            CompareAndAssert("sport/tennis/player1/ranking", "sport/tennis/+", false);

            CompareAndAssert("sport", "sport/#", true);
            CompareAndAssert("sport", "sport/+", false);
            CompareAndAssert("sport/", "sport/+", true);
        }

        [TestMethod]
        public void TopicFilterComparer_SingleLevel_Finance()
        {
            // Tests from official MQTT spec (4.7.1.3 Single level wildcard)
            CompareAndAssert("/finance", "+/+", true);
            CompareAndAssert("/finance", "/+", true);
            CompareAndAssert("/finance", "+", false);
        }

        private static void CompareAndAssert(string topic, string filter, bool expectedResult)
        {
            Assert.AreEqual(expectedResult, MqttTopicFilterComparer.IsMatch(topic, filter));
        }
    }
}
