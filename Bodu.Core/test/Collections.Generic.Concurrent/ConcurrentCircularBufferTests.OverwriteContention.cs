// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBufferTests.OverwriteContention.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentCircularBufferTests
{
    /// <summary>
    /// Verifies that under heavy concurrent producer contention on a full overwrite buffer, no data is lost or
    /// duplicated: every enqueued value is accounted for exactly once as either still present in the buffer or
    /// evicted (each eviction firing <see cref="ConcurrentCircularBuffer{T}.ItemEvicted" /> once), and the buffer
    /// never exceeds its capacity.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This pins the safety contract of the overwrite path under contention. It does <b>not</b> assert one eviction
    /// per admission: under concurrent producers a single logical "enqueue into a full buffer" can trigger more than
    /// one eviction (a producer may evict, then lose the freed slot to another producer and evict again), so the
    /// <see cref="ConcurrentCircularBuffer{T}.ItemEvicted" /> count is an upper bound on distinct admissions, not a
    /// one-to-one signal. What is guaranteed is conservation — every value is present-or-evicted exactly once — which
    /// is what this test asserts.
    /// </para>
    /// </remarks>
    [TestMethod]
    [TestCategory("Stress")]
    public void OverwriteContention_WhenManyProducersOverflowFullBuffer_ShouldConserveEveryValueExactlyOnce()
    {
        const int capacity = 8;
        const int producerCount = 4;
        const int perProducer = 5_000;
        int totalEnqueued = producerCount * perProducer;

        var buffer = new ConcurrentCircularBuffer<TestItem>(capacity, allowOverwrite: true);
        var evicted = new ConcurrentBag<int>();
        buffer.ItemEvicted += item => evicted.Add(item.Value);

        // Each producer enqueues a disjoint block of values so every value across the run is unique.
        var producers = new Thread[producerCount];
        for (int p = 0; p < producerCount; p++)
        {
            int start = p * perProducer;
            producers[p] = new Thread(() =>
            {
                for (int i = 0; i < perProducer; i++)
                    buffer.Enqueue(new TestItem(start + i));
            });
        }

        foreach (Thread t in producers) t.Start();
        foreach (Thread t in producers) t.Join();

        TestItem[] survivors = buffer.ToArray();

        // Capacity is never exceeded.
        Assert.IsLessThanOrEqualTo(capacity, survivors.Length,
            "The buffer must never hold more than its capacity.");

        // Conservation: survivors + evictions == everything enqueued, with no value counted twice.
        var seen = new HashSet<int>();
        foreach (TestItem item in survivors)
            Assert.IsTrue(seen.Add(item.Value), $"Value {item.Value} appears more than once among survivors.");
        foreach (int value in evicted)
            Assert.IsTrue(seen.Add(value), $"Value {value} was both evicted and present, or evicted twice.");

        Assert.AreEqual(totalEnqueued, seen.Count,
            "Every enqueued value must be accounted for exactly once as either a survivor or an eviction " +
            $"(survivors={survivors.Length}, evicted={evicted.Count}, expected total={totalEnqueued}).");
    }

    /// <summary>
    /// Verifies the one-eviction-per-admission contract in the uncontended (single-threaded) case: each enqueue into
    /// a full overwrite buffer evicts exactly one element, so the eviction count equals the number of overflow
    /// admissions.
    /// </summary>
    [TestMethod]
    public void OverwriteContention_WhenSingleThreadedOverflow_ShouldEvictExactlyOncePerAdmission()
    {
        const int capacity = 4;
        const int extra = 10; // admissions beyond the first `capacity` that must each evict one element

        var buffer = new ConcurrentCircularBuffer<TestItem>(capacity, allowOverwrite: true);
        int evictions = 0;
        buffer.ItemEvicted += _ => evictions++;

        for (int i = 0; i < capacity + extra; i++)
            buffer.Enqueue(new TestItem(i));

        Assert.AreEqual(extra, evictions,
            "Single-threaded overflow must evict exactly one element per admission beyond capacity.");
    }
}
