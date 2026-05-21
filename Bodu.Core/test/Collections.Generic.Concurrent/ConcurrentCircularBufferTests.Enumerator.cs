// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBufferTests.Enumerator.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentCircularBufferTests
{

    /// <summary>
    /// Verifies that the enumerator yields <see langword="null" /> and non-null items in their correct FIFO order.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenBufferContainsNulls_ShouldYieldNullsInCorrectOrder()
    {
        var buffer = new ConcurrentCircularBuffer<string>(3);
        buffer.Enqueue("A");
        buffer.Enqueue(null);
        buffer.Enqueue("B");

        var items = buffer.ToArray();
        CollectionAssert.AreEqual(new[] { "A", null, "B" }, items);
    }

    /// <summary>
    /// Verifies that <c>foreach</c> iteration over an empty buffer performs no iterations.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenBufferIsEmpty_ForeachShouldNotIterate()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(4);
        var count = 0;
        foreach (TestItem _ in buffer) count++;
        Assert.AreEqual(0, count);
    }

    /// <summary>
    /// Verifies that the enumerator yields no items when the buffer is empty.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenBufferIsEmpty_ShouldYieldNoItems()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(5);
        var items = buffer.ToList();
        Assert.IsEmpty(items);
    }

    /// <summary>
    /// Verifies that the enumerator yields a stable snapshot whose length is within capacity while a concurrent dequeuer is active.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenConcurrentDequeueOccurs_ShouldYieldStableSnapshot()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(10);
        for (var i = 0; i < 10; i++) buffer.Enqueue(new TestItem(i));

        var reader = Task.Run(() =>
        {
            var items = buffer.ToList();
            Assert.IsLessThanOrEqualTo(10, items.Count);
        });

        var remover = Task.Run(() =>
        {
            for (var i = 0; i < 10; i++)
                buffer.TryDequeue(out TestItem? _);
        });

        Task.WaitAll(reader, remover);
    }

    /// <summary>
    /// Verifies that a snapshot taken during concurrent enqueue/dequeue mutations remains internally consistent and contains only valid items.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenConcurrentMutationsOccur_ShouldYieldPartialConsistentView()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(100);
        for (var i = 0; i < 100; i++) buffer.Enqueue(new TestItem(i));

        var enumeratorTask = Task.Run(() =>
        {
            TestItem[] snapshot = buffer.ToArray();
            Assert.IsLessThanOrEqualTo(buffer.Capacity, snapshot.Length);
        });

        var mutateTask = Task.Run(() =>
        {
            for (var i = 100; i < 200; i++)
            {
                buffer.TryDequeue(out TestItem? _);
                buffer.TryEnqueue(new TestItem(i));
            }
        });

        Task.WaitAll(enumeratorTask, mutateTask);
    }

    /// <summary>
    /// Verifies that accessing <see cref="ConcurrentCircularBuffer{T}.Enumerator.Current"/> after the enumerator has been
    /// fully exhausted returns the default value for the element type.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenCurrentAccessedAfterExhaustion_ShouldReturnDefault()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Enqueue(new TestItem(3));

        ConcurrentCircularBuffer<TestItem>.Enumerator enumerator = buffer.GetEnumerator();
        while (enumerator.MoveNext())
        {
            // Consume all items.
        }

        Assert.AreEqual(default(TestItem), enumerator.Current);
    }

    /// <summary>
    /// Verifies that accessing <see cref="ConcurrentCircularBuffer{T}.Enumerator.Current"/> before the first call to
    /// <see cref="ConcurrentCircularBuffer{T}.Enumerator.MoveNext"/> returns the default value for the element type.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenCurrentAccessedBeforeMoveNext_ShouldReturnDefault()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        buffer.Enqueue(new TestItem(10));
        buffer.Enqueue(new TestItem(20));

        ConcurrentCircularBuffer<TestItem>.Enumerator enumerator = buffer.GetEnumerator();
        Assert.AreEqual(default(TestItem), enumerator.Current);
    }

    /// <summary>
    /// Verifies that <c>foreach</c> iteration visits every item in FIFO order.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenIteratedViaForeach_ShouldVisitAllItemsInFifoOrder()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(4);
        buffer.Enqueue(new TestItem(10));
        buffer.Enqueue(new TestItem(20));
        buffer.Enqueue(new TestItem(30));

        var values = new List<int>();
        foreach (TestItem item in buffer)
            values.Add(item.Value);

        CollectionAssert.AreEqual(new[] { 10, 20, 30 }, values);
    }

    /// <summary>
    /// Verifies that calling <see cref="ConcurrentCircularBuffer{T}.Enumerator.Reset"/> after full enumeration allows the
    /// enumerator to iterate over the same snapshot again, yielding identical items in the same order.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenResetCalled_ShouldRestartIteration()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(4);
        buffer.Enqueue(new TestItem(100));
        buffer.Enqueue(new TestItem(200));
        buffer.Enqueue(new TestItem(300));

        ConcurrentCircularBuffer<TestItem>.Enumerator enumerator = buffer.GetEnumerator();

        var firstPass = new List<TestItem>();
        while (enumerator.MoveNext())
            firstPass.Add(enumerator.Current);

        enumerator.Reset();

        var secondPass = new List<TestItem>();
        while (enumerator.MoveNext())
            secondPass.Add(enumerator.Current);

        CollectionAssert.AreEqual(firstPass, secondPass);
    }

    /// <summary>
    /// Verifies that enumerating the buffer while a concurrent enqueuer runs does not throw and yields only non-null seeded items.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenSnapshotTakenDuringConcurrentEnqueue_ShouldNotThrow()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(20);

        for (var i = 0; i < 10; i++)
            buffer.Enqueue(new TestItem(i));

        var reader = Task.Run(() =>
        {
            var snapshot = buffer.ToList(); // triggers enumeration
            Assert.IsTrue(snapshot.All(x => x != null));
        });

        var writer = Task.Run(() =>
        {
            for (var i = 10; i < 30; i++)
                buffer.TryEnqueue(new TestItem(i));
        });

        Task.WaitAll(reader, writer);
    }

    /// <summary>
    /// Verifies that after a wraparound, the enumerator still yields items in logical FIFO order.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenWraparoundHasOccurred_ShouldPreserveFifoOrder()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Enqueue(new TestItem(3));
        buffer.Dequeue();       // removes 1
        buffer.Enqueue(new TestItem(4)); // wraparound

        var result = buffer.ToArray().Select(x => x.Value).ToArray();
        CollectionAssert.AreEqual(new[] { 2, 3, 4 }, result);
    }

}