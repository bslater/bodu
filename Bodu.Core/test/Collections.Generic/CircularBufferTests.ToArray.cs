// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CircularBufferTests.ToArray.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic
{
    public partial class CircularBufferTests
    {
        /// <summary>
        /// Verifies that ToArray returns buffer contents in the correct logical FirstInFirstOut order.
        /// </summary>
        [TestMethod]
        public void ToArray_WhenBufferHasElements_ShouldReturnElementsInOrder()
        {
            var buffer = new CircularBuffer<string>(3);
            buffer.Enqueue("x");
            buffer.Enqueue("y");

            CollectionAssert.AreEqual(new[] { "x", "y" }, buffer.ToArray());
        }

        /// <summary>
        /// Verifies that ToArray returns an empty array when the buffer has no items.
        /// </summary>
        [TestMethod]
        public void ToArray_WhenBufferIsEmpty_ShouldReturnEmptyArray()
        {
            var buffer = new CircularBuffer<object>(5);
            var actual = buffer.ToArray();

            Assert.AreEqual(0, actual.Length, "Expected an empty array when buffer is empty.");
        }

        /// <summary>
        /// Verifies that ToArray returns items in FirstInFirstOut order even when the internal
        /// buffer is wrapped.
        /// </summary>
        [TestMethod]
        public void ToArray_WhenBufferIsFullAndWrapped_ShouldReturnInFifoOrder()
        {
            var buffer = new CircularBuffer<int>(3);
            buffer.Enqueue(10);
            buffer.Enqueue(20);
            buffer.Enqueue(30);
            buffer.Dequeue(); // Remove 10
            buffer.Enqueue(40); // Wrap around

            CollectionAssert.AreEqual(new[] { 20, 30, 40 }, buffer.ToArray());
        }

        /// <summary>
        /// Verifies that modifying the array returned by ToArray does not affect the contents of the buffer.
        /// </summary>
        [TestMethod]
        public void ToArray_WhenSnapshotIsModified_ShouldNotAffectBuffer()
        {
            var buffer = new CircularBuffer<int>(3);
            buffer.Enqueue(10);
            buffer.Enqueue(20);
            buffer.Enqueue(30);

            int[] snapshot = buffer.ToArray();
            snapshot[0] = 999;

            int[] actual = buffer.ToArray();
            Assert.AreEqual(10, actual[0]);
        }
    }
}