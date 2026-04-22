// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CircularBufferTests.ItemEvicting.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

﻿namespace Bodu.Collections.Generic
{
    public partial class CircularBufferTests
    {
        /// <summary>
        /// Verifies that ItemEvicting is triggered with the correct items before they are overwritten.
        /// </summary>
        [TestMethod]
        public void ItemEvicting_WhenOverwriteOccurs_ShouldContainCorrectItems()
        {
            var evictedItems = new List<string>();
            var buffer = new CircularBuffer<string>(2, allowOverwrite: true);
            buffer.ItemEvicting += item => evictedItems.Add(item);

            buffer.Enqueue("A");
            buffer.Enqueue("B");
            buffer.Enqueue("C"); // Should evict "A"
            buffer.Enqueue("D"); // Should evict "B"

            CollectionAssert.AreEqual(new[] { "A", "B" }, evictedItems);
        }

        /// <summary>
        /// Verifies that ItemEvicting is not triggered when overwrite is disabled.
        /// </summary>
        [TestMethod]
        public void ItemEvicting_WhenOverwriteIsDisabled_ShouldNotFire()
        {
            bool anyEventFired = false;
            var buffer = new CircularBuffer<int>(2, allowOverwrite: false);
            buffer.ItemEvicting += _ => anyEventFired = true;

            buffer.Enqueue(1);
            buffer.Enqueue(2);
            var success = buffer.TryEnqueue(3); // Should not enqueue or fire event

            Assert.IsFalse(success);
            Assert.IsFalse(anyEventFired);
        }

        /// <summary>
        /// Verifies that ItemEvicting is not triggered when the buffer has available capacity.
        /// </summary>
        [TestMethod]
        public void ItemEvicting_WhenBufferHasAvailableCapacity_ShouldNotFire()
        {
            bool anyEventFired = false;
            var buffer = new CircularBuffer<int>(3, allowOverwrite: true);
            buffer.ItemEvicting += _ => anyEventFired = true;

            buffer.Enqueue(1);
            buffer.Enqueue(2); // Buffer not full yet

            Assert.IsFalse(anyEventFired);
        }

        /// <summary>
        /// Verifies that ItemEvicting is triggered before ItemEvicted when an item is overwritten.
        /// </summary>
        [TestMethod]
        public void ItemEvicting_WhenItemOverwritten_ShouldOccurBeforeItemEvicted()
        {
            var sequence = new List<string>();

            var buffer = new CircularBuffer<string>(2, allowOverwrite: true);
            buffer.ItemEvicting += item => sequence.Add($"Evicting:{item}");
            buffer.ItemEvicted += item => sequence.Add($"Evicted:{item}");

            buffer.Enqueue("1");
            buffer.Enqueue("2");
            buffer.Enqueue("3"); // Evict "1"

            CollectionAssert.AreEqual(new[] { "Evicting:1", "Evicted:1" }, sequence);
        }

        /// <summary>
        /// Verifies that an exception thrown in the ItemEvicting handler is propagated by the buffer.
        /// </summary>
        [TestMethod]
        public void ItemEvicting_WhenHandlerThrows_ShouldPropagateException()
        {
            var buffer = new CircularBuffer<string>(2, allowOverwrite: true);
            buffer.Enqueue("X");
            buffer.Enqueue("Y");

            buffer.ItemEvicting += _ => throw new InvalidOperationException("Eviction vetoed");

            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                buffer.Enqueue("Z"); // Overwrites "X" → triggers ItemEvicting
            });
        }
    }
}