using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentCircularBufferTests
{
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

    [TestMethod]
    public void Enumerator_WhenBufferIsEmpty_ShouldYieldNoItems()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(5);
        var items = buffer.ToList();
        Assert.AreEqual(0, items.Count);
    }

    [TestMethod]
    public void Enumerator_WhenIteratedViaForeach_ShouldVisitAllItemsInFifoOrder()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(4);
        buffer.Enqueue(new TestItem(10));
        buffer.Enqueue(new TestItem(20));
        buffer.Enqueue(new TestItem(30));

        var values = new List<int>();
        foreach (var item in buffer)
            values.Add(item.Value);

        CollectionAssert.AreEqual(new[] { 10, 20, 30 }, values);
    }

    [TestMethod]
    public void Enumerator_WhenBufferIsEmpty_ForeachShouldNotIterate()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(4);
        var count = 0;
        foreach (var _ in buffer) count++;
        Assert.AreEqual(0, count);
    }

    [TestMethod]
    public void Enumerator_WhenConcurrentDequeueOccurs_ShouldYieldStableSnapshot()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(10);
        for (int i = 0; i < 10; i++) buffer.Enqueue(new TestItem(i));

        var reader = Task.Run(() =>
        {
            var items = buffer.ToList();
            Assert.IsTrue(items.Count <= 10);
        });

        var remover = Task.Run(() =>
        {
            for (int i = 0; i < 10; i++)
                buffer.TryDequeue(out TestItem? _);
        });

        Task.WaitAll(reader, remover);
    }

    [TestMethod]
    public void Enumerator_WhenConcurrentMutationsOccur_ShouldYieldPartialConsistentView()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(100);
        for (int i = 0; i < 100; i++) buffer.Enqueue(new TestItem(i));

        var enumeratorTask = Task.Run(() =>
        {
            var snapshot = buffer.ToArray();
            Assert.IsTrue(snapshot.Length <= buffer.Capacity);
            Assert.IsTrue(snapshot.All(x => x is TestItem or null));
        });

        var mutateTask = Task.Run(() =>
        {
            for (int i = 100; i < 200; i++)
            {
                buffer.TryDequeue(out TestItem? _);
                buffer.TryEnqueue(new TestItem(i));
            }
        });

        Task.WaitAll(enumeratorTask, mutateTask);
    }

    [TestMethod]
    public void Enumerator_WhenSnapshotTakenDuringConcurrentEnqueue_ShouldNotThrow()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(20);

        for (int i = 0; i < 10; i++)
            buffer.Enqueue(new TestItem(i));

        var reader = Task.Run(() =>
        {
            var snapshot = buffer.ToList(); // triggers enumeration
            Assert.IsTrue(snapshot.All(x => x != null));
        });

        var writer = Task.Run(() =>
        {
            for (int i = 10; i < 30; i++)
                buffer.TryEnqueue(new TestItem(i));
        });

        Task.WaitAll(reader, writer);
    }

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

    /// <summary>
    /// Verifies that accessing <see cref="ConcurrentCircularBuffer{T}.Enumerator.Current"/> before the first call to
    /// <see cref="ConcurrentCircularBuffer{T}.Enumerator.MoveNext"/> returns the default value for the element type.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenCurrentAccessedBeforeMoveNext_ShouldReturnDefault()
    {
        var buffer = new ConcurrentCircularBuffer<int>(3);
        buffer.Enqueue(10);
        buffer.Enqueue(20);

        var enumerator = buffer.GetEnumerator();
        Assert.AreEqual(default(int), enumerator.Current);
    }

    /// <summary>
    /// Verifies that accessing <see cref="ConcurrentCircularBuffer{T}.Enumerator.Current"/> after the enumerator has been
    /// fully exhausted returns the default value for the element type.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenCurrentAccessedAfterExhaustion_ShouldReturnDefault()
    {
        var buffer = new ConcurrentCircularBuffer<int>(3);
        buffer.Enqueue(1);
        buffer.Enqueue(2);
        buffer.Enqueue(3);

        var enumerator = buffer.GetEnumerator();
        while (enumerator.MoveNext())
        {
            // Consume all items.
        }

        Assert.AreEqual(default(int), enumerator.Current);
    }

    /// <summary>
    /// Verifies that calling <see cref="ConcurrentCircularBuffer{T}.Enumerator.Reset"/> after full enumeration allows the
    /// enumerator to iterate over the same snapshot again, yielding identical items in the same order.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenResetCalled_ShouldRestartIteration()
    {
        var buffer = new ConcurrentCircularBuffer<int>(4);
        buffer.Enqueue(100);
        buffer.Enqueue(200);
        buffer.Enqueue(300);

        var enumerator = buffer.GetEnumerator();

        var firstPass = new List<int>();
        while (enumerator.MoveNext())
            firstPass.Add(enumerator.Current);

        enumerator.Reset();

        var secondPass = new List<int>();
        while (enumerator.MoveNext())
            secondPass.Add(enumerator.Current);

        CollectionAssert.AreEqual(firstPass, secondPass);
    }
}