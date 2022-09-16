// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Implementations;
using MQTTnet.Internal;

namespace MQTTnet.Tests.Internal
{
    [TestClass]
    public sealed class AsyncEvent_Tests
    {
        int _testEventAsyncCount;

        [TestMethod]
        public async Task Attach_EventHandler()
        {
            var testClass = new TestClass();

            testClass.TestEventAsync += OnTestEventAsync;

            await testClass.FireEventAsync(EventArgs.Empty);

            Assert.AreEqual(1, _testEventAsyncCount);
        }

        [TestMethod]
        public async Task Detach_EventHandler()
        {
            var testClass = new TestClass();

            testClass.TestEventAsync += OnTestEventAsync;
            testClass.TestEventAsync -= OnTestEventAsync;

            await testClass.FireEventAsync(EventArgs.Empty);

            Assert.AreEqual(0, _testEventAsyncCount);
        }

        [TestMethod]
        public void Has_Handlers()
        {
            var testClass = new TestClass();
            testClass.TestEventAsync += OnTestEventAsync;

            Assert.AreEqual(true, testClass.HasTestHandlers);
        }

        [TestMethod]
        public void No_Handlers()
        {
            var testClass = new TestClass();

            Assert.AreEqual(false, testClass.HasTestHandlers);
        }

        [TestMethod]
        public void Remove_Handlers()
        {
            var testClass = new TestClass();
            testClass.TestEventAsync += OnTestEventAsync;
            testClass.TestEventAsync -= OnTestEventAsync;

            Assert.AreEqual(false, testClass.HasTestHandlers);
        }

        Task OnTestEventAsync(EventArgs arg)
        {
            Interlocked.Increment(ref _testEventAsyncCount);
            return PlatformAbstractionLayer.CompletedTask;
        }

        sealed class TestClass
        {
            readonly AsyncEvent<EventArgs> _testEvent = new AsyncEvent<EventArgs>();

            public event Func<EventArgs, Task> TestEventAsync
            {
                add => _testEvent.AddHandler(value);
                remove => _testEvent.RemoveHandler(value);
            }

            public bool HasTestHandlers => _testEvent.HasHandlers;

            public Task FireEventAsync(EventArgs eventArgs)
            {
                return _testEvent.InvokeAsync(eventArgs);
            }
        }
    }
}