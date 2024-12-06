using System;
using System.Collections;
using System.Collections.Generic;

namespace MQTTnet.Tests.Mockups
{
    public class TestEnvironmentCollection : IReadOnlyCollection<TestEnvironment>, IDisposable
    {
        private readonly TestEnvironment[] _testEnvironments;

        public int Count => _testEnvironments.Length;

        public TestEnvironmentCollection(params TestEnvironment[] testEnvironments)
        {
            _testEnvironments = testEnvironments;
        }

        public IEnumerator<TestEnvironment> GetEnumerator()
        {
            foreach (var environment in _testEnvironments)
            {
                yield return environment;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            foreach (var environment in _testEnvironments)
            {
                environment.Dispose();
            }
        }
    }
}
