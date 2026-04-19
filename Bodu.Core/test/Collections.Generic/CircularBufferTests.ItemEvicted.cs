namespace Bodu.Collections.Generic
{
    public partial class CircularBufferTests
    {
        /// <summary>
        /// Verifies that ItemEvicted is not raised when the buffer has capacity available.
        /// </summary>
        [TestMethod]
        public void ItemEvicted_WhenBufferHasCapacity_ShouldNotFire()
        {
            bool anyEventFired = false;
            var buffer = new CircularBuffer<int>(3, allowOverwrite: true);
            buffer.ItemEvicted += _ => anyEventFired = true;

            buffer.Enqueue(1);
            buffer.Enqueue(2); // Buffer not full

            Assert.IsFalse(anyEventFired);
        }

        /// <summary>
        /// Verifies that ItemEvicted handlers are invoked without exception under high concurrency.
        /// </summary>
        [TestMethod]
        public void ItemEvicted_WhenConcurrentOverwrite_ShouldNotThrow()
        {
            var buffer = new CircularBuffer<int>(10, allowOverwrite: true);
            using var evicted = new ManualResetEventSlim(false);

            buffer.ItemEvicted += (evictedItem) => evicted.Set();

            // Fill buffer to enable overwriting
            for (int i = 0; i < buffer.Capacity; i++)
                buffer.Enqueue(i);

            // Start concurrent enqueuing that will trigger overwrite
            var writer = Task.Run(() =>
            {
                for (int i = 0; i < 100; i++)
                    buffer.Enqueue(i);
            });

            writer.Wait();

            Assert.IsTrue(evicted.Wait(1000), "Eviction event was not triggered during concurrent overwrite.");
        }

        /// <summary>
        /// Verifies that an exception thrown in the ItemEvicted handler is propagated by the buffer.
        /// </summary>
        [TestMethod]
        public void ItemEvicted_WhenHandlerThrows_ShouldPropagateException()
        {
            var buffer = new CircularBuffer<string>(2, allowOverwrite: true);
            buffer.Enqueue("A");
            buffer.Enqueue("B");

            buffer.ItemEvicted += _ => throw new InvalidOperationException("Simulated error");

            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                buffer.Enqueue("C"); // Overwrites "A" → triggers ItemEvicted
            });
        }

        /// <summary>
        /// Verifies that all registered ItemEvicted handlers are invoked.
        /// </summary>
        [TestMethod]
        public void ItemEvicted_WhenMultipleHandlers_ShouldInvokeAll()
        {
            var log = new List<string>();
            var buffer = new CircularBuffer<string>(2, allowOverwrite: true);

            buffer.ItemEvicted += item => log.Add("Handler1:" + item);
            buffer.ItemEvicted += item => log.Add("Handler2:" + item);

            buffer.Enqueue("X");
            buffer.Enqueue("Y");
            buffer.Enqueue("Z"); // Overwrite X

            CollectionAssert.AreEqual(new[] { "Handler1:X", "Handler2:X" }, log);
        }

        /// <summary>
        /// Verifies that ItemEvicted is not raised when overwrite is not allowed.
        /// </summary>
        [TestMethod]
        public void ItemEvicted_WhenOverwriteIsDisabled_ShouldNotFire()
        {
            bool anyEventFired = false;
            var buffer = new CircularBuffer<int>(2, allowOverwrite: false);
            buffer.ItemEvicted += _ => anyEventFired = true;

            buffer.Enqueue(1);
            buffer.Enqueue(2);
            var success = buffer.TryEnqueue(3); // Should not overwrite or fire event

            Assert.IsFalse(success);
            Assert.IsFalse(anyEventFired);
        }

        /// <summary>
        /// Verifies that ItemEvicted is raised with the correct item when an element is overwritten.
        /// </summary>
        [TestMethod]
        public void ItemEvicted_WhenOverwriteOccurs_ShouldContainCorrectItem()
        {
            var evictedItems = new List<string>();
            var buffer = new CircularBuffer<string>(2, allowOverwrite: true);
            buffer.ItemEvicted += item => evictedItems.Add(item);

            buffer.Enqueue("X");
            buffer.Enqueue("Y");
            buffer.Enqueue("Z"); // Should evict "X"

            CollectionAssert.AreEqual(new[] { "X" }, evictedItems);
        }

        /// <summary>
        /// Verifies that an unsubscribed ItemEvicted handler does not receive further eviction events.
        /// </summary>
        [TestMethod]
        public void ItemEvicted_WhenHandlerUnsubscribed_ShouldNotReceiveFurtherEvents()
        {
            int callCount = 0;
            var buffer = new CircularBuffer<int>(1, allowOverwrite: true);

            Action<int> handler = _ => callCount++;
            buffer.ItemEvicted += handler;

            buffer.Enqueue(1);
            buffer.Enqueue(2); // evicts 1 — handler fires

            Assert.AreEqual(1, callCount);

            buffer.ItemEvicted -= handler;

            buffer.Enqueue(3); // evicts 2 — handler should not fire

            Assert.AreEqual(1, callCount);
        }

        /// <summary>
        /// Verifies that the ItemEvicted handler receives a null argument when a null item is evicted.
        /// </summary>
        [TestMethod]
        public void ItemEvicted_WhenNullItemIsEvicted_ShouldReceiveNullArgument()
        {
            string? received = "sentinel";
            bool handlerCalled = false;

            var buffer = new CircularBuffer<string?>(1, allowOverwrite: true);
            buffer.ItemEvicted += item =>
            {
                received = item;
                handlerCalled = true;
            };

            buffer.Enqueue(null);
            buffer.Enqueue("X"); // evicts null

            Assert.IsTrue(handlerCalled);
            Assert.IsNull(received);
        }
    }
}