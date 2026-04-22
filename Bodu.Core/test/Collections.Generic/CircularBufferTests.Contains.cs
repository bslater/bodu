// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CircularBufferTests.Contains.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic
{
    public partial class CircularBufferTests
    {
        /// <summary>
        /// Verifies that Contains returns true when the specified item exists in the buffer.
        /// </summary>
        [TestMethod]
        public void Contains_WhenItemExists_ShouldReturnTrue()
        {
            var buffer = new CircularBuffer<string>(3);
            buffer.Enqueue("x");
            Assert.IsTrue(buffer.Contains("x"));
        }

        /// <summary>
        /// Verifies that Contains uses the default equality comparer for value types.
        /// </summary>
        [TestMethod]
        public void Contains_WhenUsingValueTypes_ShouldUseDefaultEquality()
        {
            var buffer = new CircularBuffer<int>(3);
            buffer.Enqueue(10);
            buffer.Enqueue(20);

            Assert.IsTrue(buffer.Contains(10));
            Assert.IsFalse(buffer.Contains(30));
        }

        /// <summary>
        /// Verifies that Contains returns false when the specified item does not exist in the buffer.
        /// </summary>
        [TestMethod]
        public void Contains_WhenItemDoesNotExist_ShouldReturnFalse()
        {
            var buffer = new CircularBuffer<int>(2);
            buffer.Enqueue(1);
            Assert.IsFalse(buffer.Contains(99));
        }

        /// <summary>
        /// Verifies that Contains returns true when null is present in the buffer.
        /// </summary>
        [TestMethod]
        public void Contains_WhenBufferContainsNull_ShouldReturnTrue()
        {
            var buffer = new CircularBuffer<object>(2);
            buffer.Enqueue(null);
            Assert.IsTrue(buffer.Contains(null));
        }

        /// <summary>
        /// Verifies that Contains returns false immediately when the buffer is empty, exercising the early-exit path.
        /// </summary>
        [TestMethod]
        public void Contains_WhenBufferIsEmpty_ShouldReturnFalse()
        {
            var buffer = new CircularBuffer<string>(4);
            Assert.IsFalse(buffer.Contains("x"));
        }

        /// <summary>
        /// Verifies that Contains finds an item located in the first physical segment of a wrapped buffer
        /// (the <c>[head .. capacity)</c> portion).
        /// </summary>
        [TestMethod]
        public void Contains_WhenBufferIsWrapped_ShouldFindItemInFirstSegment()
        {
            // Layout after setup: capacity=3, head=1, tail=1 (full).
            // Physical slots: [0]='D', [1]='B', [2]='C'
            // Logical FIFO:   B, C, D
            // First segment (head=1 to end): B, C — at physical indices 1 and 2.
            var buffer = new CircularBuffer<string>(3);
            buffer.Enqueue("A");
            buffer.Enqueue("B");
            buffer.Enqueue("C");
            buffer.Dequeue();        // removes "A"; head advances to 1
            buffer.Enqueue("D");     // fills slot 0; tail wraps to 1

            Assert.IsTrue(buffer.Contains("B"), "Expected to find 'B' in the first physical segment.");
            Assert.IsTrue(buffer.Contains("C"), "Expected to find 'C' in the first physical segment.");
        }

        /// <summary>
        /// Verifies that Contains finds an item located in the second physical segment of a wrapped buffer
        /// (the <c>[0 .. tail)</c> portion).
        /// </summary>
        [TestMethod]
        public void Contains_WhenBufferIsWrapped_ShouldFindItemInSecondSegment()
        {
            // Same layout: physical slot [0]='D' is the second segment (0 to tail=1).
            var buffer = new CircularBuffer<string>(3);
            buffer.Enqueue("A");
            buffer.Enqueue("B");
            buffer.Enqueue("C");
            buffer.Dequeue();        // removes "A"
            buffer.Enqueue("D");     // wraps into slot 0

            Assert.IsTrue(buffer.Contains("D"), "Expected to find 'D' in the second physical segment.");
        }

        /// <summary>
        /// Verifies that Contains returns false for an item that is absent from a wrapped buffer, confirming both
        /// physical segments are searched and correctly report no match.
        /// </summary>
        [TestMethod]
        public void Contains_WhenBufferIsWrappedAndItemAbsent_ShouldReturnFalse()
        {
            var buffer = new CircularBuffer<string>(3);
            buffer.Enqueue("A");
            buffer.Enqueue("B");
            buffer.Enqueue("C");
            buffer.Dequeue();        // removes "A"
            buffer.Enqueue("D");     // wraps into slot 0

            Assert.IsFalse(buffer.Contains("A"), "Dequeued item should not be found.");
            Assert.IsFalse(buffer.Contains("Z"), "Item never enqueued should not be found.");
        }

        /// <summary>
        /// Verifies that Contains correctly searches a full buffer where head and tail coincide, covering the
        /// case where the entire capacity forms a single wrapped span.
        /// </summary>
        [TestMethod]
        public void Contains_WhenBufferIsFull_ShouldFindAllItems()
        {
            var buffer = new CircularBuffer<int>(4);
            buffer.Enqueue(10);
            buffer.Enqueue(20);
            buffer.Enqueue(30);
            buffer.Enqueue(40);  // full; head == tail == 0

            Assert.IsTrue(buffer.Contains(10));
            Assert.IsTrue(buffer.Contains(20));
            Assert.IsTrue(buffer.Contains(30));
            Assert.IsTrue(buffer.Contains(40));
            Assert.IsFalse(buffer.Contains(99));
        }

        /// <summary>
        /// Verifies that Contains returns false for previously present items after the buffer has been cleared.
        /// </summary>
        [TestMethod]
        public void Contains_AfterClear_ShouldReturnFalseForPreviouslyPresentItems()
        {
            var buffer = new CircularBuffer<string>(3);
            buffer.Enqueue("A");
            buffer.Enqueue("B");

            buffer.Clear();

            Assert.IsFalse(buffer.Contains("A"));
            Assert.IsFalse(buffer.Contains("B"));
        }

        /// <summary>
        /// Verifies that Contains returns false for an item that has been evicted by an overwrite and true
        /// for the item that replaced it.
        /// </summary>
        [TestMethod]
        public void Contains_WhenItemEvictedByOverwrite_ShouldReturnFalse()
        {
            var buffer = new CircularBuffer<int>(2, allowOverwrite: true);
            buffer.Enqueue(1);
            buffer.Enqueue(2);
            buffer.Enqueue(3); // evicts 1

            Assert.IsFalse(buffer.Contains(1));
            Assert.IsTrue(buffer.Contains(3));
        }

        /// <summary>
        /// Verifies that Contains returns true when duplicate items are present and at least one instance remains
        /// after a dequeue.
        /// </summary>
        [TestMethod]
        public void Contains_WhenDuplicatesPresent_ShouldReturnTrueWhileAnyInstanceExists()
        {
            var buffer = new CircularBuffer<string>(3);
            buffer.Enqueue("X");
            buffer.Enqueue("X");

            buffer.Dequeue(); // removes first "X"

            Assert.IsTrue(buffer.Contains("X"));
        }
    }
}