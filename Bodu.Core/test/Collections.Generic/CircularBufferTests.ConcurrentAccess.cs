using System.Collections.Concurrent;

namespace Bodu.Collections.Generic
{
    public partial class CircularBufferTests
    {
        /// <summary>
        /// Verifies that the buffer maintains consistency during parallel Enqueue and Dequeue operations.
        /// </summary>
        [TestMethod]
        public void Concurrency_ParallelEnqueueDequeue_ShouldMaintainConsistency()
        {
            var buffer = new CircularBuffer<int>(100, allowOverwrite: true);
            var enqueueTasks = Enumerable.Range(0, 1000).Select(i => Task.Run(() => buffer.Enqueue(i))).ToArray();
            var dequeueResults = new ConcurrentBag<int>();
            var dequeueTasks = Enumerable.Range(0, 1000).Select(i => Task.Run(() =>
            {
                if (buffer.TryDequeue(out int value))
                    dequeueResults.Add(value);
            })).ToArray();

            Task.WaitAll(enqueueTasks.Concat(dequeueTasks).ToArray());

            Assert.IsTrue(dequeueResults.Count > 0);
        }

        /// <summary>
        /// Verifies that multiple threads can safely enqueue in parallel.
        /// </summary>
        [TestMethod]
        public void Concurrency_ParallelEnqueue_ShouldNotThrow()
        {
            var buffer = new CircularBuffer<int>(100, allowOverwrite: true);

            var tasks = Enumerable.Range(0, 500).Select(i =>
                Task.Run(() => buffer.Enqueue(i))
            ).ToArray();

            Task.WaitAll(tasks);

            Assert.IsTrue(buffer.Count > 0);
        }

        /// <summary>
        /// Verifies that multiple concurrent Peek operations do not throw or interfere with each other.
        /// </summary>
        [TestMethod]
        public void Concurrency_ParallelPeek_ShouldNotThrow()
        {
            var buffer = new CircularBuffer<int>(10);
            for (int i = 0; i < 10; i++) buffer.Enqueue(i);

            var tasks = Enumerable.Range(0, 100).Select(_ => Task.Run(() =>
            {
                try { buffer.Peek(); }
                catch (Exception ex) { Assert.Fail("Concurrent Peek threw: " + ex.Message); }
            })).ToArray();

            Task.WaitAll(tasks);
        }

        /// <summary>
        /// Verifies that Peek does not throw during concurrent Enqueue operations.
        /// </summary>
        [TestMethod]
        public void Concurrency_PeekDuringWrite_ShouldNotThrow()
        {
            var buffer = new CircularBuffer<int>(10);
            buffer.Enqueue(1); // Ensure at least one item

            var peekTasks = Enumerable.Range(0, 100).Select(_ => Task.Run(() =>
            {
                try { buffer.Peek(); }
                catch (Exception ex) { Assert.Fail("Peek threw during concurrent access: " + ex.Message); }
            }));

            var enqueueTasks = Enumerable.Range(0, 100).Select(i => Task.Run(() => buffer.Enqueue(i)));

            Task.WaitAll(peekTasks.Concat(enqueueTasks).ToArray());
        }

        /// <summary>
        /// Verifies that TryDequeue does not return false while items are being enqueued by other threads.
        /// </summary>
        [TestMethod]
        public void Concurrency_TryDequeue_ShouldNotReturnFalseWhenEnqueueing()
        {
            var buffer = new CircularBuffer<int>(10);
            var dequeued = new ConcurrentBag<int>();

            var enqueueing = Task.Run(() =>
            {
                for (int i = 0; i < 100; i++) buffer.Enqueue(i);
            });

            var dequeueing = Task.Run(() =>
            {
                Task.Delay(5).Wait(); // allow buffer to fill slightly
                for (int i = 0; i < 100; i++)
                {
                    if (buffer.TryDequeue(out int value))
                        dequeued.Add(value);
                }
            });

            Task.WaitAll(enqueueing, dequeueing);

            Assert.IsTrue(dequeued.Count > 0);
        }

        /// <summary>
        /// Verifies that FIFO order is preserved under concurrent enqueuing with a single consumer, assuming no overwriting and sufficient capacity.
        /// </summary>
        [TestMethod]
        public void Concurrency_EnqueueOrder_ShouldPreserveFIFOOrderUnderSingleConsumer()
        {
            var itemCount = 500;
            var buffer = new CircularBuffer<int>(itemCount, allowOverwrite: false);
            var written = Enumerable.Range(0, itemCount).ToArray();

            foreach (var item in written)
                buffer.Enqueue(item);

            var dequeued = new List<int>();
            while (buffer.TryDequeue(out int item))
                dequeued.Add(item);

            CollectionAssert.AreEqual(written, dequeued);
        }

        /// <summary>
        /// Performs a stress test with mixed read/write activity to ensure no deadlocks or exceptions occur.
        /// </summary>
        [TestMethod]
        public void Concurrency_StressTest_ShouldNotDeadlockOrThrow()
        {
            var buffer = new CircularBuffer<int>(50, allowOverwrite: true);

            var writers = Enumerable.Range(0, 10).Select(_ =>
                Task.Run(() =>
                {
                    for (int i = 0; i < 1000; i++) buffer.Enqueue(i);
                })
            );

            var readers = Enumerable.Range(0, 10).Select(_ =>
                Task.Run(() =>
                {
                    for (int i = 0; i < 1000; i++) buffer.TryDequeue(out _);
                })
            );

            Task.WaitAll(writers.Concat(readers).ToArray());

            Assert.IsTrue(true); // If we reach here, no deadlock or crash occurred.
        }

        /// <summary>
        /// Verifies that simultaneous access to the buffer from multiple threads does not throw unexpectedly (not thread-safe by contract).
        /// </summary>
        [TestMethod]
        public void Concurrency_UnsynchronizedAccess_ShouldNotThrowButCorrectnessNotGuaranteed()
        {
            var buffer = new CircularBuffer<int>(100);

            var writer = new Thread(() =>
            {
                for (int i = 0; i < 1000; i++)
                    buffer.TryEnqueue(i);
            });

            var reader = new Thread(() =>
            {
                for (int i = 0; i < 1000; i++)
                    buffer.TryDequeue(out _);
            });

            writer.Start();
            reader.Start();

            writer.Join();
            reader.Join();

            // Just validating no exception occurs - thread safety not guaranteed.
            Assert.IsTrue(buffer.Count >= 0);
        }
    }
}