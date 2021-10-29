using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Server;

namespace MQTTnet.Tests.Server
{
    [TestClass]
    public sealed class MqttTopicFilterComparer_Tests
    {
        [TestMethod]
        public void Plus_Match_With_Separator_Only()
        {
            //A Topic Name or Topic Filter consisting only of the ‘/’ character is valid
            CompareAndAssert("A", "+", MqttTopicFilterCompareResult.IsMatch);
        }

        [TestMethod]
        public void Hash_Match_With_Separator_Only()
        {
            //A Topic Name or Topic Filter consisting only of the ‘/’ character is valid
            CompareAndAssert("/", "#", MqttTopicFilterCompareResult.IsMatch);
        }

        [TestMethod]
        public void DirectMatch()
        {
            CompareAndAssert("A/B/C", "A/B/C", MqttTopicFilterCompareResult.IsMatch);
        }

        [TestMethod]
        public void DirectNoMatch()
        {
            CompareAndAssert("A/B/X", "A/B/C", MqttTopicFilterCompareResult.NoMatch);
        }

        [TestMethod]
        public void MiddleOneLevelWildcardMatch()
        {
            CompareAndAssert("A/B/C", "A/+/C", MqttTopicFilterCompareResult.IsMatch);
        }

        [TestMethod]
        public void MiddleOneLevelWildcardNoMatch()
        {
            CompareAndAssert("A/B/C/D", "A/+/C", MqttTopicFilterCompareResult.NoMatch);
        }

        [TestMethod]
        public void BeginningOneLevelWildcardMatch()
        {
            CompareAndAssert("A/B/C", "+/B/C", MqttTopicFilterCompareResult.IsMatch);
        }

        [TestMethod]
        public void EndOneLevelWildcardMatch()
        {
            CompareAndAssert("A/B/C", "A/B/+", MqttTopicFilterCompareResult.IsMatch);
        }

        [TestMethod]
        public void EndMultipleLevelsWildcardMatch()
        {
            CompareAndAssert("A/B/C", "A/#", MqttTopicFilterCompareResult.IsMatch);
        }

        [TestMethod]
        public void EndMultipleLevelsWildcardNoMatch()
        {
            CompareAndAssert("A/B/C/D", "A/C/#", MqttTopicFilterCompareResult.NoMatch);
        }

        [TestMethod]
        public void EndMultipleLevelsWildcardMatchEmptyLevel()
        {
            CompareAndAssert("A/", "A/#", MqttTopicFilterCompareResult.IsMatch);
        }

        [TestMethod]
        public void AllLevelsWildcardMatch()
        {
            CompareAndAssert("A/B/C/D", "#", MqttTopicFilterCompareResult.IsMatch);
        }

        [TestMethod]
        public void MultiLevel_Sport()
        {
            // Tests from official MQTT spec (4.7.1.2 Multi-level wildcard)
            CompareAndAssert("sport/tennis/player1", "sport/tennis/player1/#", MqttTopicFilterCompareResult.IsMatch);
            CompareAndAssert("sport/tennis/player1/ranking", "sport/tennis/player1/#", MqttTopicFilterCompareResult.IsMatch);
            CompareAndAssert("sport/tennis/player1/score/wimbledon", "sport/tennis/player1/#", MqttTopicFilterCompareResult.IsMatch);

            CompareAndAssert("sport/tennis/player1", "sport/tennis/+", MqttTopicFilterCompareResult.IsMatch);
            CompareAndAssert("sport/tennis/player2", "sport/tennis/+", MqttTopicFilterCompareResult.IsMatch);
            CompareAndAssert("sport/tennis/player1/ranking", "sport/tennis/+", MqttTopicFilterCompareResult.NoMatch);

            CompareAndAssert("sport", "sport/#", MqttTopicFilterCompareResult.IsMatch);
            CompareAndAssert("sport", "sport/+", MqttTopicFilterCompareResult.NoMatch);
            CompareAndAssert("sport/", "sport/+", MqttTopicFilterCompareResult.IsMatch);
        }

        [TestMethod]
        public void SingleLevel_Finance()
        {
            // Tests from official MQTT spec (4.7.1.3 Single level wildcard)
            CompareAndAssert("/finance", "+/+", MqttTopicFilterCompareResult.IsMatch);
            CompareAndAssert("/finance", "/+", MqttTopicFilterCompareResult.IsMatch);
            CompareAndAssert("/finance", "+", MqttTopicFilterCompareResult.NoMatch);
        }
        
        [TestMethod]
        public void Reserved_Multi_Level_Wildcard_Only()
        {
            CompareAndAssert("$SPECIAL/TOPIC", "#", MqttTopicFilterCompareResult.NoMatch);
        }
        
        [TestMethod]
        public void Reserved_Single_Level_Wildcard_Prefix()
        {
            CompareAndAssert("$SYS/monitor/Clients", "+/monitor/Clients", MqttTopicFilterCompareResult.NoMatch);
        }
        
        [TestMethod]
        public void Reserved_Single_Level_Wildcard_Suffix()
        {
            CompareAndAssert("$SYS/monitor/Clients", "$SYS/monitor/+", MqttTopicFilterCompareResult.IsMatch);
        }
        
        [TestMethod]
        public void Reserved_Single_Level_Wildcard()
        {
            CompareAndAssert("$SYS/monitor/Clients", "$SYS/#", MqttTopicFilterCompareResult.IsMatch);
        }
        
        static void CompareAndAssert(string topic, string filter, MqttTopicFilterCompareResult expectedResult)
        {
            Assert.AreEqual(expectedResult, MqttTopicFilterComparer.Compare(topic, filter));
        }
    }
}
