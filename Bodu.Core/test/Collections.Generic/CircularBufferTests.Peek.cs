namespace Bodu.Collections.Generic
{
    public partial class CircularBufferTests
    {
        /// <summary>
        /// Verifies that Peek returns the oldest item in the buffer without removing it.
        /// </summary>
        [TestMethod]
        public void Peek_WhenBufferHasItems_ShouldReturnOldestWithoutRemoving()
        {
            var buffer = new CircularBuffer<int>(2);
            buffer.Enqueue(100);
            var peek = buffer.Peek();

            Assert.AreEqual(100, peek, "Peek should return the oldest item.");
            Assert.AreEqual(1, buffer.Count, "Peek should not remove the item.");
        }

        /// <summary>
        /// Verifies that Peek returns the correct item after the buffer has wrapped around.
        /// </summary>
        [TestMethod]
        public void Peek_WhenBufferHasWrapped_ShouldReturnOldestItem()
        {
            var buffer = new CircularBuffer<string>(3);
            buffer.Enqueue("A");
            buffer.Enqueue("B");
            buffer.Enqueue("C");
            buffer.Dequeue();           // Removes "A"
            buffer.Enqueue("D");        // Wraparound

            var peek = buffer.Peek();

            Assert.AreEqual("B", peek);
        }

        /// <summary>
        /// Verifies that Peek throws InvalidOperationException when the buffer is empty.
        /// </summary>
        [TestMethod]
        public void Peek_WhenBufferIsEmpty_ShouldThrowExactly()
        {
            var buffer = new CircularBuffer<string>(2);

            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                _ = buffer.Peek();
            });
        }

        /// <summary>
        /// Verifies that calling Peek repeatedly does not change the count of the buffer.
        /// </summary>
        [TestMethod]
        public void Peek_WhenCalledRepeatedly_ShouldNotChangeCount()
        {
            var buffer = new CircularBuffer<int>(5);
            buffer.Enqueue(1);
            buffer.Enqueue(2);
            buffer.Enqueue(3);

            int expectedCount = buffer.Count;

            for (int i = 0; i < 100; i++)
            {
                buffer.Peek();
            }

            Assert.AreEqual(expectedCount, buffer.Count);
        }

        /// <summary>
        /// Verifies that Peek reflects the new oldest item after a dequeue operation.
        /// </summary>
        [TestMethod]
        public void Peek_WhenOldestChangesDueToDequeue_ShouldReflectNewOldest()
        {
            var buffer = new CircularBuffer<string>(3);
            buffer.Enqueue("A");
            buffer.Enqueue("B");

            Assert.AreEqual("A", buffer.Peek());

            buffer.Dequeue();

            Assert.AreEqual("B", buffer.Peek());
        }
    }
}