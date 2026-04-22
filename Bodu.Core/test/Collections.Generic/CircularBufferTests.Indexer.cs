// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CircularBufferTests.Indexer.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic
{
    public partial class CircularBufferTests
    {
        /// <summary>
        /// Verifies that the indexer retrieves the correct element in FirstInFirstOut order.
        /// </summary>
        [TestMethod]
        public void Indexer_WhenAccessingValidIndices_ShouldReturnElementsInFifoOrder()
        {
            var buffer = new CircularBuffer<int>(3);
            buffer.Enqueue(10);
            buffer.Enqueue(20);
            buffer.Enqueue(30);

            Assert.AreEqual(10, buffer[0]);
            Assert.AreEqual(20, buffer[1]);
            Assert.AreEqual(30, buffer[2]);
        }

        /// <summary>
        /// Verifies that accessing an index equal to Count throws ArgumentOutOfRangeException.
        /// </summary>
        [TestMethod]
        public void Indexer_WhenIndexEqualsCount_ShouldThrowExactly()
        {
            var buffer = new CircularBuffer<int>(3);
            buffer.Enqueue(1);
            buffer.Enqueue(2);

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = buffer[buffer.Count];
            });
        }

        /// <summary>
        /// Verifies that accessing a negative index throws ArgumentOutOfRangeException.
        /// </summary>
        [TestMethod]
        public void Indexer_WhenIndexIsNegative_ShouldThrowExactly()
        {
            var buffer = new CircularBuffer<int>(2);
            buffer.Enqueue(10);

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = buffer[-1];
            });
        }

        /// <summary>
        /// Verifies that accessing an index greater than Count throws ArgumentOutOfRangeException.
        /// </summary>
        [TestMethod]
        public void Indexer_WhenIndexExceedsCount_ShouldThrowExactly()
        {
            var buffer = new CircularBuffer<int>(3);
            buffer.Enqueue(1);
            buffer.Enqueue(2);

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = buffer[99];
            });
        }

        /// <summary>
        /// Verifies that accessing an unused index throws ArgumentOutOfRangeException.
        /// </summary>
        [TestMethod]
        public void Indexer_WhenIndexIsUnused_ShouldThrowExactly()
        {
            var buffer = new CircularBuffer<string>(2);
            buffer.Enqueue("x");

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = buffer[1];
            });
        }
    }
}