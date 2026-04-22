// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CircularBufferTests.Clear.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic
{
    public partial class CircularBufferTests
    {
        /// <summary>
        /// Verifies that Clear removes all items from the buffer and resets its state.
        /// </summary>
        [TestMethod]
        public void Clear_ShouldClearAllItems()
        {
            var buffer = new CircularBuffer<int>(3);
            buffer.Enqueue(1);
            buffer.Enqueue(2);
            buffer.Clear();

            Assert.AreEqual(0, buffer.Count);

            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                _ = buffer.Dequeue(); // should throw
            });
        }

        /// <summary>
        /// Verifies that Clear is a no-op when the buffer is already empty.
        /// </summary>
        [TestMethod]
        public void Clear_WhenBufferIsAlreadyEmpty_ShouldDoNothing()
        {
            var buffer = new CircularBuffer<string>(3);
            buffer.Clear(); // no items

            Assert.AreEqual(0, buffer.Count);
            Assert.IsTrue(buffer.ToArray().Length == 0);
        }

        /// <summary>
        /// Verifies that Clear resets buffer state even after wraparound.
        /// </summary>
        [TestMethod]
        public void Clear_WhenCalledAfterWraparound_ShouldResetInternalState()
        {
            var buffer = new CircularBuffer<int>(3);
            buffer.Enqueue(1);
            buffer.Enqueue(2);
            buffer.Enqueue(3);
            buffer.Dequeue();
            buffer.Enqueue(4); // wrap
            buffer.Clear();

            Assert.AreEqual(0, buffer.Count);
            Assert.AreEqual(3, buffer.Capacity);
            CollectionAssert.AreEqual(Array.Empty<int>(), buffer.ToArray());
        }

        /// <summary>
        /// Verifies that calling Clear during an active enumeration throws <see cref="InvalidOperationException" />.
        /// </summary>
        [TestMethod]
        public void Clear_WhenEnumerationIsActive_ShouldInvalidateEnumerator()
        {
            var buffer = new CircularBuffer<int>(Enumerable.Range(1, 10));

            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                foreach (var item in buffer)
                {
                    if (item == 5)
                        buffer.Clear();
                }
            });
        }

        /// <summary>
        /// Verifies that calling Clear before advancing an enumerator causes the next MoveNext call to throw
        /// <see cref="InvalidOperationException" />.
        /// </summary>
        [TestMethod]
        public void Clear_WhenCalledBeforeMoveNext_ShouldInvalidateEnumerator()
        {
            var buffer = new CircularBuffer<int>(3);
            buffer.Enqueue(1);
            buffer.Enqueue(2);
            buffer.Enqueue(3);

            var enumerator = buffer.GetEnumerator();

            // Clear before any MoveNext — version is now stale
            buffer.Clear();

            Assert.ThrowsExactly<InvalidOperationException>(() => enumerator.MoveNext());
        }

        /// <summary>
        /// Verifies that calling Clear mid-enumeration causes Reset to throw <see cref="InvalidOperationException" />.
        /// </summary>
        [TestMethod]
        public void Clear_WhenCalledDuringEnumeration_ShouldInvalidateReset()
        {
            var buffer = new CircularBuffer<int>(3);
            buffer.Enqueue(1);
            buffer.Enqueue(2);
            buffer.Enqueue(3);

            var enumerator = buffer.GetEnumerator();
            enumerator.MoveNext(); // advance past first element

            buffer.Clear();

            Assert.ThrowsExactly<InvalidOperationException>(() => enumerator.Reset());
        }

        /// <summary>
        /// Verifies that Clear removes all elements when the buffer is not wrapped (head &lt; tail).
        /// </summary>
        [TestMethod]
        public void Clear_WhenNotWrapped_ShouldRemoveAllItems()
        {
            var buffer = new CircularBuffer<string>(5);
            buffer.Enqueue("A");
            buffer.Enqueue("B");
            buffer.Enqueue("C");

            buffer.Clear();

            Assert.AreEqual(0, buffer.Count);
            Assert.IsTrue(buffer.ToArray().Length == 0);
        }

        /// <summary>
        /// Verifies that Clear zeroes both array segments when the buffer is wrapped (head &gt;= tail).
        /// </summary>
        [TestMethod]
        public void Clear_WhenWrapped_ShouldZeroAllSegments()
        {
            var buffer = new CircularBuffer<int>(5);

            buffer.Enqueue(1);
            buffer.Enqueue(2);
            buffer.Enqueue(3);
            buffer.Enqueue(4);
            buffer.Enqueue(5);
            buffer.Dequeue(); // move head forward
            buffer.Enqueue(6); // causes wrap

            Assert.IsTrue(buffer.Count > 0);

            buffer.Clear();

            Assert.AreEqual(0, buffer.Count);
            CollectionAssert.AreEqual(Array.Empty<int>(), buffer.ToArray());
        }

        /// <summary>
        /// Verifies that clearing a full buffer does not raise any eviction events.
        /// </summary>
        [TestMethod]
        public void Clear_WhenBufferFull_ShouldNotRaiseEvictionEvents()
        {
            int evictingCount = 0;
            int evictedCount = 0;

            var buffer = new CircularBuffer<int>(3, allowOverwrite: true);
            buffer.Enqueue(1);
            buffer.Enqueue(2);
            buffer.Enqueue(3);

            buffer.ItemEvicting += _ => evictingCount++;
            buffer.ItemEvicted += _ => evictedCount++;

            buffer.Clear();

            Assert.AreEqual(0, evictingCount);
            Assert.AreEqual(0, evictedCount);
        }

        /// <summary>
        /// Verifies that clearing and immediately reusing the buffer preserves its capacity and maintains
        /// correct FIFO ordering for subsequently enqueued items.
        /// </summary>
        [TestMethod]
        public void Clear_WhenCalledAndImmediatelyReused_ShouldPreserveCapacityAndMaintainFifoOrder()
        {
            var buffer = new CircularBuffer<int>(3);
            buffer.Enqueue(1);
            buffer.Enqueue(2);
            buffer.Enqueue(3);

            buffer.Clear();

            buffer.Enqueue(10);
            buffer.Enqueue(20);

            Assert.AreEqual(3, buffer.Capacity);
            Assert.AreEqual(2, buffer.Count);
            CollectionAssert.AreEqual(new[] { 10, 20 }, buffer.ToArray());
        }

        /// <summary>
        /// Verifies that enqueuing items after clearing a previously full buffer behaves correctly
        /// and maintains FIFO order.
        /// </summary>
        [TestMethod]
        public void Clear_WhenBufferWasFullAndNewItemsEnqueued_ShouldMaintainFifoOrder()
        {
            var buffer = new CircularBuffer<int>(3, allowOverwrite: true);
            buffer.Enqueue(1);
            buffer.Enqueue(2);
            buffer.Enqueue(3);

            Assert.AreEqual(3, buffer.Count);
            buffer.Clear();
            Assert.AreEqual(0, buffer.Count);

            buffer.Enqueue(10);
            buffer.Enqueue(20);
            buffer.Enqueue(30);

            CollectionAssert.AreEqual(new[] { 10, 20, 30 }, buffer.ToArray());
        }

        /// <summary>
        /// Verifies that draining all items and then refilling the buffer preserves FIFO order
        /// across the cycle.
        /// </summary>
        [TestMethod]
        public void Clear_WhenDrainedAndRefilled_ShouldPreserveFifoOrder()
        {
            var buffer = new CircularBuffer<int>(3);
            buffer.Enqueue(1);
            buffer.Enqueue(2);
            buffer.Enqueue(3);

            buffer.Dequeue();
            buffer.Dequeue();
            buffer.Dequeue();

            Assert.AreEqual(0, buffer.Count);

            buffer.Enqueue(10);
            buffer.Enqueue(20);

            CollectionAssert.AreEqual(new[] { 10, 20 }, buffer.ToArray());
        }
    }
}