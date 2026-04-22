// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CircularBufferTests.TryDequeue.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic
{
    public partial class CircularBufferTests
    {
        /// <summary>
        /// Verifies that TryDequeue returns true and dequeues the oldest item when the buffer is
        /// not empty.
        /// </summary>
        [TestMethod]
        public void TryDequeue_WhenBufferIsNotEmpty_ShouldReturnTrueAndDequeueOldestItem()
        {
            var buffer = new CircularBuffer<int>(3);
            buffer.Enqueue(100);
            buffer.Enqueue(200);

            var success = buffer.TryDequeue(out int actual);

            Assert.IsTrue(success);
            Assert.AreEqual(100, actual);
            Assert.AreEqual(1, buffer.Count);
        }

        /// <summary>
        /// Verifies that <see cref="CircularBuffer{T}.TryDequeue" /> returns the correct value after an overwrite has occurred.
        /// </summary>
        [TestMethod]
        public void TryDequeue_AfterOverwrite_ShouldReturnCorrectItem()
        {
            var buffer = new CircularBuffer<int>(2, allowOverwrite: true);
            buffer.TryEnqueue(1);
            buffer.TryEnqueue(2);
            buffer.TryEnqueue(3); // Overwrites 1

            var success = buffer.TryDequeue(out int value);

            Assert.IsTrue(success);
            Assert.AreEqual(2, value);
        }

        /// <summary>
        /// Verifies that <see cref="CircularBuffer{T}.TryDequeue" /> returns false and outputs default value when the buffer is empty.
        /// </summary>
        [TestMethod]
        public void TryDequeue_WhenBufferIsEmpty_ShouldReturnFalseAndDefaultValue()
        {
            var buffer = new CircularBuffer<object>(1);

            var success = buffer.TryDequeue(out object actual);

            Assert.IsFalse(success);
            Assert.IsNull(actual);
        }

        /// <summary>
        /// Verifies that <see cref="CircularBuffer{T}.TryDequeue" /> returns true and outputs null when null was enqueued.
        /// </summary>
        [TestMethod]
        public void TryDequeue_WhenNullWasEnqueued_ShouldReturnTrueAndOutputNull()
        {
            var buffer = new CircularBuffer<object>(1);
            buffer.Enqueue(null);

            var success = buffer.TryDequeue(out var actual);

            Assert.IsTrue(success);
            Assert.IsNull(actual);
        }

        /// <summary>
        /// Verifies that <see cref="CircularBuffer{T}.TryDequeue" /> does not trigger any eviction events.
        /// </summary>
        [TestMethod]
        public void TryDequeue_WhenCalled_ShouldNotTriggerEvictionEvents()
        {
            var events = new List<string>();
            var buffer = new CircularBuffer<string>(2);

            buffer.TryEnqueue("A");
            buffer.TryEnqueue("B");

            buffer.ItemEvicting += item => events.Add("Evicting:" + item);
            buffer.ItemEvicted += item => events.Add("Evicted:" + item);

            var actual = buffer.TryDequeue(out string item);

            Assert.IsTrue(actual);
            Assert.AreEqual("A", item);
            CollectionAssert.AreEqual(Array.Empty<string>(), events);
            CollectionAssert.AreEqual(new[] { "B" }, buffer.ToArray());
        }

        /// <summary>
        /// Verifies that <see cref="CircularBuffer{T}.TryDequeue" /> returns false after all items have been removed from the buffer.
        /// </summary>
        [TestMethod]
        public void TryDequeue_WhenBufferTransitionsToEmpty_ShouldReturnFalseAfterLastItem()
        {
            var buffer = new CircularBuffer<int>(3);
            buffer.Enqueue(1);
            buffer.Enqueue(2);

            bool first = buffer.TryDequeue(out int value1);
            bool second = buffer.TryDequeue(out int value2);
            bool third = buffer.TryDequeue(out int value3);

            Assert.IsTrue(first);
            Assert.AreEqual(1, value1);
            Assert.IsTrue(second);
            Assert.AreEqual(2, value2);
            Assert.IsFalse(third);
        }
    }
}