namespace Bodu.Collections.Generic
{
    public partial class CircularBufferTests
    {
        /// <summary>
        /// Verifies that Enqueue adds a new item to the end of the buffer.
        /// </summary>
        [TestMethod]
        public void Enqueue_WhenBufferHasSpace_ShouldAddItemToEnd()
        {
            var buffer = new CircularBuffer<int>(2);
            buffer.Enqueue(10);

            Assert.AreEqual(10, buffer.Peek());
        }

        /// <summary>
        /// Verifies that Enqueue replaces the single element when buffer size is 1 and overwrite is enabled.
        /// </summary>
        [TestMethod]
        public void Enqueue_WhenBufferHasSingleSlot_ShouldReplaceExistingOnOverwrite()
        {
            var buffer = new CircularBuffer<string>(1, allowOverwrite: true);
            buffer.Enqueue("A");
            buffer.Enqueue("B"); // should evict "A"

            CollectionAssert.AreEqual(new[] { "B" }, buffer.ToArray());
        }

        /// <summary>
        /// Verifies that Enqueue overwrites the oldest item when the buffer is full and overwrite is allowed.
        /// </summary>
        [TestMethod]
        public void Enqueue_WhenFullAndOverwriteAllowed_ShouldOverwriteOldest()
        {
            var buffer = new CircularBuffer<int>(2, allowOverwrite: true);
            buffer.Enqueue(1);
            buffer.Enqueue(2);
            buffer.Enqueue(3);

            CollectionAssert.AreEqual(new[] { 2, 3 }, buffer.ToArray());
        }

        /// <summary>
        /// Verifies that Enqueue throws an exception when the buffer is full and overwrite is not allowed.
        /// </summary>
        [TestMethod]
        public void Enqueue_WhenFullAndOverwriteNotAllowed_ShouldThrowExactly()
        {
            var buffer = new CircularBuffer<int>(2, allowOverwrite: false);
            buffer.Enqueue(1);
            buffer.Enqueue(2);

            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                buffer.Enqueue(3); // should throw
            });
        }

        /// <summary>
        /// Verifies that Enqueue allows duplicate items to be stored.
        /// </summary>
        [TestMethod]
        public void Enqueue_WhenDuplicatesProvided_ShouldStoreAll()
        {
            var buffer = new CircularBuffer<int>(3);
            buffer.Enqueue(5);
            buffer.Enqueue(5);
            buffer.Enqueue(5);

            CollectionAssert.AreEqual(new[] { 5, 5, 5 }, buffer.ToArray());
        }

        /// <summary>
        /// Verifies that Enqueue can reuse space freed by a prior Dequeue operation.
        /// </summary>
        [TestMethod]
        public void Enqueue_WhenSlotIsFreed_ShouldReuseSpace()
        {
            var buffer = new CircularBuffer<string>(2);
            buffer.Enqueue("a");
            buffer.Dequeue();
            buffer.Enqueue("b");

            CollectionAssert.AreEqual(new[] { "b" }, buffer.ToArray());
        }

        /// <summary>
        /// Verifies that Enqueue correctly stores null values for reference types.
        /// </summary>
        [TestMethod]
        public void Enqueue_WhenNullValueProvided_ShouldAcceptNullReference()
        {
            var buffer = new CircularBuffer<string>(2);
            buffer.Enqueue(null);

            Assert.AreEqual(1, buffer.Count);
            Assert.IsNull(buffer.Peek());
        }

        /// <summary>
        /// Verifies that Enqueue throws an InvalidOperationException when the buffer is full and overwrite is disabled.
        /// </summary>
        [TestMethod]
        public void Enqueue_WhenBufferIsFullAndOverwriteDisabled_ShouldThrowExactly()
        {
            var buffer = new CircularBuffer<string>(2, allowOverwrite: false);
            buffer.Enqueue("A");
            buffer.Enqueue("B");

            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                buffer.Enqueue("C");
            });
        }

        /// <summary>
        /// Verifies that head and tail wrap correctly when the buffer is full and overwrite is enabled.
        /// </summary>
        [TestMethod]
        public void Enqueue_WhenWraparoundOccurs_ShouldMaintainOrder()
        {
            var buffer = new CircularBuffer<int>(3, allowOverwrite: true);
            buffer.Enqueue(1);
            buffer.Enqueue(2);
            buffer.Enqueue(3);
            buffer.Dequeue(); // advance head
            buffer.Enqueue(4); // wraps around

            CollectionAssert.AreEqual(new[] { 2, 3, 4 }, buffer.ToArray());
        }

        /// <summary>
        /// Verifies that multiple null values can be enqueued and retained in order.
        /// </summary>
        [TestMethod]
        public void Enqueue_WhenMultipleNullsProvided_ShouldRetainInOrder()
        {
            var buffer = new CircularBuffer<string>(3);
            buffer.Enqueue(null);
            buffer.Enqueue("X");
            buffer.Enqueue(null);

            CollectionAssert.AreEqual(new[] { null, "X", null }, buffer.ToArray());
        }
    }
}