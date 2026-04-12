using System.Collections.Concurrent;

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentCircularBufferTests
{
    [TestMethod]
    public void TryDequeue_WhenAllowOverwriteTrue_ShouldNotThrowAndRespectBounds()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(5, allowOverwrite: true);

        // Overfill heavily; no exceptions expected
        Parallel.For(0, 1000, i => buffer.Enqueue(new TestItem(i)));

        // Drain what's there
        int count = 0;
        while (buffer.TryDequeue(out _)) count++;

        Assert.IsTrue(count >= 0 && count <= buffer.Capacity);
        Assert.AreEqual(0, buffer.Count);
    }

    [TestMethod]
    public void TryDequeue_WhenBufferIsEmpty_ShouldReturnFalse()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        Assert.IsFalse(buffer.TryDequeue(out _));
    }

    [TestMethod]
    public void TryDequeue_WhenBufferTransitionsToEmpty_ShouldReturnFalseAfterLastItem()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(2);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));

        Assert.IsTrue(buffer.TryDequeue(out _));
        Assert.IsTrue(buffer.TryDequeue(out _));
        Assert.IsFalse(buffer.TryDequeue(out _));
    }

    // Previously tested with capacity = 1. Migrated to capacity = 2 — the minimum supported
    // value — following the implementation change that requires capacity >= 2 for the Vyukov
    // MPMC sequence protocol to be correct.

    [TestMethod]
    public void TryDequeue_WhenCapacityIsMinimum_ShouldBehaveConsistently()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(2, allowOverwrite: true);

        for (int i = 0; i < 100; i++)
        {
            buffer.Enqueue(new TestItem(i));
            _ = buffer.TryDequeue(out _);
        }

        Assert.IsTrue(buffer.Count >= 0 && buffer.Count <= 2);
    }

    [TestMethod]
    public void TryDequeue_WhenClearInterleaves_ShouldReturnFalseOnceEmptiedWithoutThrowing()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(8);
        for (int i = 0; i < 6; i++) buffer.Enqueue(new TestItem(i));

        var exceptions = new ConcurrentBag<Exception>();
        var done = new ManualResetEventSlim(false);

        var clearer = Task.Run(() =>
        {
            for (int i = 0; i < 100; i++)
            {
                buffer.Clear();
                Thread.SpinWait(20);
            }
            done.Set();
        });

        var reader = Task.Run(() =>
        {
            try
            {
                // Keep trying while clears run; should never throw
                while (!done.IsSet)
                    _ = buffer.TryDequeue(out _);
            }
            catch (Exception ex) { exceptions.Add(ex); }
        });

        Task.WaitAll(clearer, reader);
        Assert.AreEqual(0, exceptions.Count);
        Assert.IsFalse(buffer.TryDequeue(out _)); // should be empty now
    }

    [TestMethod]
    public void TryDequeue_WhenConcurrentEnqueueInterleaves_ShouldEventuallySucceed()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(10);
        var dequeuedCount = 0;
        var success = 0;

        var writer = Task.Run(() =>
        {
            for (int i = 0; i < 20; i++)
            {
                buffer.Enqueue(new TestItem(i));
                Thread.Sleep(1);
            }
        });

        var reader = Task.Run(() =>
        {
            for (int i = 0; i < 50; i++)
            {
                if (buffer.TryDequeue(out var item) && item != null)
                {
                    Interlocked.Increment(ref dequeuedCount);
                    Interlocked.Exchange(ref success, 1);
                }
                Thread.Sleep(1);
            }
        });

        Task.WaitAll(writer, reader);

        Assert.AreEqual(1, success, "TryDequeue never succeeded during concurrent enqueue.");
        Assert.IsTrue(dequeuedCount > 0, "Expected to dequeue at least one item.");
    }

    [TestMethod]
    public void TryDequeue_WhenManyConsumersAgainstPreloadedBuffer_ShouldDrainAllItems()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(20);
        for (int i = 0; i < 20; i++) buffer.Enqueue(new TestItem(i));

        var dequeued = new ConcurrentBag<int>();

        Parallel.For(0, 20, _ =>
        {
            if (buffer.TryDequeue(out var item) && item != null)
                dequeued.Add(item.Value);
        });

        Assert.AreEqual(20, dequeued.Count);
        CollectionAssert.AreEquivalent(Enumerable.Range(0, 20).ToArray(), dequeued.OrderBy(x => x).ToArray());
    }

    [TestMethod]
    public void TryDequeue_WhenMultipleConsumers_ShouldDequeueEachItemAtMostOnce()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(30);
        for (int i = 0; i < 30; i++) buffer.Enqueue(new TestItem(i));

        var dequeued = new ConcurrentBag<int>();
        var tasks = Enumerable.Range(0, 5).Select(_ => Task.Run(() =>
        {
            while (buffer.TryDequeue(out var item))
                if (item != null) dequeued.Add(item.Value);
        })).ToArray();

        Task.WaitAll(tasks);

        Assert.AreEqual(30, dequeued.Count);

        var groups = dequeued.GroupBy(x => x).Select(g => g.Count()).ToArray();
        Assert.IsTrue(groups.All(c => c == 1));
        CollectionAssert.AreEquivalent(Enumerable.Range(0, 30).ToArray(), dequeued.OrderBy(x => x).ToArray());
    }

    [TestMethod]
    public void TryDequeue_WhenNullsPresent_ShouldReturnNullValues()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem?>(4);
        buffer.Enqueue(null);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(null);

        int nulls = 0, nonNulls = 0;
        while (buffer.TryDequeue(out var item))
        {
            if (item is null) nulls++; else nonNulls++;
        }

        Assert.AreEqual(2, nulls);
        Assert.AreEqual(1, nonNulls);
    }

    [TestMethod]
    public void TryDequeue_WhenSingleThreaded_ShouldReturnInFifoOrder()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(5);
        for (int i = 0; i < 3; i++) buffer.Enqueue(new TestItem(i));

        var results = new TestItem[3];
        for (int i = 0; i < 3; i++)
            Assert.IsTrue(buffer.TryDequeue(out results[i]));

        CollectionAssert.AreEqual(new[] { 0, 1, 2 }, results.Select(r => r.Value).ToArray());
    }
}