// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBufferTests._MultipleInstances.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentCircularBufferTests
{
        /// <summary>
        /// Verifies that several independent <see cref="ConcurrentCircularBuffer{T}" /> instances operated in parallel each keep their own count within capacity without interference.
        /// </summary>
        [TestMethod]
        public void MultipleInstances_WhenAccessedInParallel_ShouldRemainThreadSafe()
        {
            var buffers = Enumerable.Range(0, 5)
                .Select(_ => new ConcurrentCircularBuffer<TestItem>(20, allowOverwrite: true))
                .ToArray();

            Parallel.ForEach(buffers, buffer =>
            {
                for (int i = 0; i < 100; i++)
                    buffer.Enqueue(new TestItem(i));

                var snapshot = buffer.ToArray();
                Assert.IsTrue(snapshot.Length <= 20, "Snapshot must not exceed capacity.");
                Assert.IsTrue(snapshot.All(x => x is not null), "No null elements expected here.");
                Assert.IsTrue(buffer.Count >= 0 && buffer.Count <= buffer.Capacity, "Count must remain within bounds.");
            });
        }

        /// <summary>
        /// Verifies that concurrent enqueues against two distinct buffers never cross-contaminate — each buffer's contents come only from its own producer.
        /// </summary>
        [TestMethod]
        public void MultipleInstances_WhenUsedConcurrently_ShouldMaintainSeparateState()
        {
            var buffer1 = new ConcurrentCircularBuffer<TestItem>(5);
            var buffer2 = new ConcurrentCircularBuffer<TestItem>(5);

            Parallel.Invoke(
                () =>
                {
                    for (int i = 0; i < 50; i++)
                        buffer1.Enqueue(new TestItem(i));
                },
                () =>
                {
                    for (int i = 100; i < 150; i++)
                        buffer2.Enqueue(new TestItem(i));
                });

            var values1 = buffer1.ToArray().Select(x => x.Value).ToArray();
            var values2 = buffer2.ToArray().Select(x => x.Value).ToArray();

            Assert.IsTrue(values1.All(v => v < 100), "Buffer1 should contain only < 100 values.");
            Assert.IsTrue(values2.All(v => v >= 100), "Buffer2 should contain only >= 100 values.");
            Assert.IsTrue(buffer1.Count <= buffer1.Capacity && buffer2.Count <= buffer2.Capacity, "Counts must remain within capacity.");
        }

        /// <summary>
        /// Verifies that <see cref="ConcurrentCircularBuffer{T}.ItemEvicted" /> handlers registered on one instance are never invoked for evictions in another instance.
        /// </summary>
        [TestMethod]
        public void MultipleInstances_WhenUsingEvents_ShouldMaintainEventIsolation()
        {
            var buffer1Events = new ConcurrentBag<string>();
            var buffer2Events = new ConcurrentBag<string>();

            var buffer1 = new ConcurrentCircularBuffer<TestItem>(2, allowOverwrite: true);
            var buffer2 = new ConcurrentCircularBuffer<TestItem>(2, allowOverwrite: true);

            buffer1.ItemEvicted += item => buffer1Events.Add("B1:" + item?.Value);
            buffer2.ItemEvicted += item => buffer2Events.Add("B2:" + item?.Value);

            buffer1.Enqueue(new TestItem(1));
            buffer1.Enqueue(new TestItem(2));
            buffer1.Enqueue(new TestItem(3)); // evicts 1

            buffer2.Enqueue(new TestItem(100));
            buffer2.Enqueue(new TestItem(200));
            buffer2.Enqueue(new TestItem(300)); // evicts 100

            Assert.IsTrue(buffer1Events.Contains("B1:1"), "Buffer1 should report evicting its own item.");
            Assert.IsTrue(buffer2Events.Contains("B2:100"), "Buffer2 should report evicting its own item.");
            Assert.IsFalse(buffer1Events.Any(e => e.StartsWith("B2:")), "Buffer1 must not receive Buffer2�s events.");
            Assert.IsFalse(buffer2Events.Any(e => e.StartsWith("B1:")), "Buffer2 must not receive Buffer1�s events.");
        }
    }
