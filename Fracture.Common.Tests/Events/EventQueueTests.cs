using System;
using System.Collections.Generic;
using Fracture.Common.Events;
using Xunit;

namespace Fracture.Common.Tests.Events
{
    [Trait("Category", "Events")]
    public sealed class EventQueueTests
    {
        #region Static fields
        private static readonly object TestTopic = new object();
        #endregion

        public delegate void TestEventHandler(object key, int value);

        [Fact]
        public void UniqueEvent_Ctor_Throws_If_Capacity_Is_Zero_Test()
            => Assert.NotNull(Record.Exception(() => new UniqueEvent<object, int>(0)));

        [Fact]
        public void UniqueEvent_Ctor_Throws_If_Capacity_Is_Less_Than_Zero_Test()
            => Assert.NotNull(Record.Exception(() => new UniqueEvent<object, int>(-1)));

        [Fact]
        public void SharedEvent_Ctor_Throws_If_Capacity_Is_Zero_Test()
            => Assert.NotNull(Record.Exception(() => new SharedEvent<object, int>(0)));

        [Fact]
        public void SharedEvent_Ctor_Throws_If_Capacity_Is_Less_Than_Zero_Test()
            => Assert.NotNull(Record.Exception(() => new SharedEvent<object, int>(-1)));

        [Fact]
        public void Shared_QueueAllows_Multiple_Letters()
        {
            // Arrange.
            var queue = new SharedEvent<object, int>(8);

            // Act.
            queue.Create(TestTopic);

            // Publish 3 events, should get invoked 3 times.
            var expectedValues = new HashSet<int>
            {
                10, 100, 1000
            };

            queue.Publish(TestTopic, 10);
            queue.Publish(TestTopic, 100);
            queue.Publish(TestTopic, 1000);

            queue.Handle((in Letter<object, int> letter) =>
            {
                Assert.Contains(letter.Args, expectedValues);

                return LetterHandlingResult.Retain;
            });
        }

        [Fact]
        public void Consumed_Letters_Are_Not_Passed_To_Handlers_Again()
        {
            // Arrange.
            var queue = new SharedEvent<object, int>(8);

            // Act.
            queue.Create(TestTopic);

            // Publish 3 events, should get invoked 3 times.
            var expectedValues = new HashSet<int>
            {
                10, 1000
            };

            queue.Publish(TestTopic, 10);
            queue.Publish(TestTopic, 100);
            queue.Publish(TestTopic, 1000);

            queue.Handle((in Letter<object, int> letter) => letter.Args == 100 ? LetterHandlingResult.Consume : LetterHandlingResult.Retain);

            // Handle again, event should now contain letters with 10 and 1000 values.
            queue.Handle((in Letter<object, int> letter) =>
            {
                Assert.Contains(letter.Args, expectedValues);

                return LetterHandlingResult.Consume;
            });

            // Handle again, should be empty now.
            queue.Handle((in Letter<object, int> letter) => throw new InvalidOperationException("event should have no letters"));
        }

        [Fact]
        public void Unique_Event_Replaces_Existing_Letters()
        {
            // Arrange.
            var queue = new UniqueEvent<object, int>(8);

            // Act.
            queue.Create(TestTopic);

            // Publish 3 events, should get invoked once.
            queue.Publish(TestTopic, 10);
            queue.Publish(TestTopic, 100);
            queue.Publish(TestTopic, 1000);

            queue.Handle((in Letter<object, int> letter) =>
            {
                Assert.Equal(1000, letter.Args);

                return LetterHandlingResult.Retain;
            });
        }
    }
}