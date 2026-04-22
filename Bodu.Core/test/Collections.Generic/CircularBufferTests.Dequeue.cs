// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CircularBufferTests.Dequeue.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic
{
    public partial class CircularBufferTests
    {
        /// <summary>
        /// Verifies that Dequeue returns and removes the oldest item in FirstInFirstOut order.
        /// </summary>
        [TestMethod]
        public void Dequeue_WhenBufferIsNotEmpty_ShouldReturnAndRemoveOldestItem()
        {
            var buffer = new CircularBuffer<int>(3);
            buffer.Enqueue(1);
            buffer.Enqueue(2);

            var actual = buffer.Dequeue();

            Assert.AreEqual(1, actual);
            Assert.AreEqual(1, buffer.Count);
        }

        /// <summary>
        /// Verifies that multiple wraparounds maintain FirstInFirstOut ordering in dequeue operations.
        /// </summary>
        [TestMethod]
        public void Dequeue_WhenMultipleWraparounds_ShouldMaintainCorrectOrder()
        {
            var buffer = new CircularBuffer<int>(3);
            buffer.Enqueue(1);
            buffer.Enqueue(2);
            buffer.Enqueue(3);
            buffer.Dequeue(); // 1
            buffer.Enqueue(4);
            buffer.Dequeue(); // 2
            buffer.Enqueue(5);

            var actual = buffer.ToArray();

            CollectionAssert.AreEqual(new[] { 3, 4, 5 }, actual);
        }

        /// <summary>
        /// Verifies that Dequeue throws InvalidOperationException when the buffer is empty.
        /// </summary>
        [TestMethod]
        public void Dequeue_WhenBufferIsEmpty_ShouldThrowExactly()
        {
            var buffer = new CircularBuffer<int>(2);

            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                buffer.Dequeue();
            });
        }

        /// <summary>
        /// Verifies that Dequeue returns null if a null value was previously enqueued.
        /// </summary>
        [TestMethod]
        public void Dequeue_WhenItemIsNull_ShouldReturnNull()
        {
            var buffer = new CircularBuffer<string>(1);
            buffer.Enqueue(null);
            var value = buffer.Dequeue();

            Assert.IsNull(value);
        }

        /// <summary>
        /// Verifies that Dequeue preserves FirstInFirstOut order after the buffer wraps around.
        /// </summary>
        [TestMethod]
        public void Dequeue_WhenBufferIsWrapped_ShouldPreserveFifoOrder()
        {
            var buffer = new CircularBuffer<int>(3);
            buffer.Enqueue(1);
            buffer.Enqueue(2);
            buffer.Enqueue(3);
            buffer.Dequeue();         // removes 1
            buffer.Enqueue(4);       // wraps around

            var result1 = buffer.Dequeue(); // expect 2
            var result2 = buffer.Dequeue(); // expect 3
            var result3 = buffer.Dequeue(); // expect 4

            CollectionAssert.AreEqual(new[] { 2, 3, 4 }, new[] { result1, result2, result3 });
            Assert.AreEqual(0, buffer.Count);
        }

        /// <summary>
        /// Verifies that Dequeue drains the buffer in FirstInFirstOut order when called multiple times.
        /// </summary>
        [TestMethod]
        public void Dequeue_WhenCalledMultipleTimes_ShouldDrainInFifoOrder()
        {
            var buffer = new CircularBuffer<string>(4);
            buffer.Enqueue("A");
            buffer.Enqueue("B");
            buffer.Enqueue("C");

            var a = buffer.Dequeue();
            var b = buffer.Dequeue();
            var c = buffer.Dequeue();

            Assert.AreEqual("A", a);
            Assert.AreEqual("B", b);
            Assert.AreEqual("C", c);
            Assert.AreEqual(0, buffer.Count);
        }

        /// <summary>
        /// Verifies that Dequeue allows reusing internal storage after wraparound.
        /// </summary>
        [TestMethod]
        public void Dequeue_WhenWraparoundOccurs_ShouldAllowSlotReuse()
        {
            var buffer = new CircularBuffer<int>(2);
            buffer.Enqueue(10);
            buffer.Dequeue();           // clears slot 0
            buffer.Enqueue(20);
            buffer.Enqueue(30);         // wraps to slot 0

            Assert.AreEqual(20, buffer.Dequeue());
            Assert.AreEqual(30, buffer.Dequeue());
            Assert.AreEqual(0, buffer.Count);
        }
    }
}