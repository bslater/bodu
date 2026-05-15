// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBufferTests.IProducerConsumerCollection.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentCircularBufferTests
{

    /// <summary>
    /// Verifies that the buffer can be wrapped by <see cref="BlockingCollection{T}"/> and that round-trip
    /// <c>Add</c>/<c>Take</c> operations work as expected.
    /// </summary>
    [TestMethod]
    public void BlockingCollection_WhenWrappingBuffer_ShouldRoundTrip()
    {
        var underlying = new ConcurrentCircularBuffer<TestItem>(capacity: 4, allowOverwrite: false);
        using var blocking = new BlockingCollection<TestItem>(underlying, boundedCapacity: 4);

        blocking.Add(new TestItem(7));
        blocking.Add(new TestItem(8));
        blocking.CompleteAdding();

        TestItem first = blocking.Take();
        TestItem second = blocking.Take();

        Assert.AreEqual(7, first.Value);
        Assert.AreEqual(8, second.Value);
        Assert.IsTrue(blocking.IsCompleted);
    }

    /// <summary>
    /// Verifies that the explicit <c>ToArray</c> and <c>CopyTo</c> members of
    /// <see cref="IProducerConsumerCollection{T}"/> agree with the public surface and reflect the buffer's
    /// FIFO order.
    /// </summary>
    [TestMethod]
    public void IProducerConsumerCollection_ToArrayAndCopyTo_ShouldMatchPublicSurface()
    {
        var concrete = new ConcurrentCircularBuffer<TestItem>(capacity: 4);
        concrete.Enqueue(new TestItem(1));
        concrete.Enqueue(new TestItem(2));
        concrete.Enqueue(new TestItem(3));
        IProducerConsumerCollection<TestItem> mvd = concrete;

        TestItem[] viaInterface = mvd.ToArray();
        TestItem[] viaConcrete = concrete.ToArray();

        Assert.AreEqual(viaConcrete.Length, viaInterface.Length);
        for (var i = 0; i < viaConcrete.Length; i++)
            Assert.AreEqual(viaConcrete[i].Value, viaInterface[i].Value);

        var copyTarget = new TestItem[5];
        mvd.CopyTo(copyTarget, index: 1);
        Assert.IsNull(copyTarget[0]);
        Assert.AreEqual(1, copyTarget[1].Value);
        Assert.AreEqual(2, copyTarget[2].Value);
        Assert.AreEqual(3, copyTarget[3].Value);
        Assert.IsNull(copyTarget[4]);
    }

    /// <summary>
    /// Verifies that <see cref="IProducerConsumerCollection{T}.TryAdd"/> returns <see langword="false"/> when
    /// the buffer is full and overwriting is disabled.
    /// </summary>
    [TestMethod]
    public void TryAdd_WhenFullAndAllowOverwriteFalse_ShouldReturnFalse()
    {
        IProducerConsumerCollection<TestItem> mvd =
            new ConcurrentCircularBuffer<TestItem>(capacity: 2, allowOverwrite: false);

        Assert.IsTrue(mvd.TryAdd(new TestItem(1)));
        Assert.IsTrue(mvd.TryAdd(new TestItem(2)));
        Assert.IsFalse(mvd.TryAdd(new TestItem(3)));
        Assert.AreEqual(2, mvd.Count);
    }

    /// <summary>
    /// Verifies that <see cref="IProducerConsumerCollection{T}.TryAdd"/> succeeds when overwriting is enabled
    /// even on a full buffer.
    /// </summary>
    [TestMethod]
    public void TryAdd_WhenFullAndAllowOverwriteTrue_ShouldReturnTrueAndEvict()
    {
        var concrete = new ConcurrentCircularBuffer<TestItem>(capacity: 2, allowOverwrite: true);
        IProducerConsumerCollection<TestItem> mvd = concrete;

        Assert.IsTrue(mvd.TryAdd(new TestItem(1)));
        Assert.IsTrue(mvd.TryAdd(new TestItem(2)));
        Assert.IsTrue(mvd.TryAdd(new TestItem(3)));

        TestItem[] snapshot = concrete.ToArray();
        Assert.AreEqual(2, snapshot.Length);
        Assert.AreEqual(2, snapshot[0].Value);
        Assert.AreEqual(3, snapshot[1].Value);
    }
    /// <summary>
    /// Verifies that <see cref="IProducerConsumerCollection{T}.TryAdd"/> forwards to <c>TryEnqueue</c> and
    /// returns <see langword="true"/> when space is available.
    /// </summary>
    [TestMethod]
    public void TryAdd_WhenSpaceAvailable_ShouldReturnTrue()
    {
        IProducerConsumerCollection<TestItem> mvd =
            new ConcurrentCircularBuffer<TestItem>(capacity: 4, allowOverwrite: false);

        Assert.IsTrue(mvd.TryAdd(new TestItem(1)));
        Assert.IsTrue(mvd.TryAdd(new TestItem(2)));
        Assert.AreEqual(2, mvd.Count);
    }

    /// <summary>
    /// Verifies that <see cref="IProducerConsumerCollection{T}.TryTake"/> returns <see langword="false"/> on an
    /// empty buffer and yields a <see langword="null"/> out value.
    /// </summary>
    [TestMethod]
    public void TryTake_WhenEmpty_ShouldReturnFalseAndNullOut()
    {
        IProducerConsumerCollection<TestItem> mvd =
            new ConcurrentCircularBuffer<TestItem>(capacity: 4);

        var taken = mvd.TryTake(out TestItem? item);

        Assert.IsFalse(taken);
        Assert.IsNull(item);
    }

    /// <summary>
    /// Verifies that <see cref="IProducerConsumerCollection{T}.TryTake"/> returns the oldest element in FIFO
    /// order when items are present.
    /// </summary>
    [TestMethod]
    public void TryTake_WhenItemPresent_ShouldReturnTrueAndOldestItem()
    {
        var concrete = new ConcurrentCircularBuffer<TestItem>(capacity: 4);
        concrete.Enqueue(new TestItem(11));
        concrete.Enqueue(new TestItem(22));
        IProducerConsumerCollection<TestItem> mvd = concrete;

        Assert.IsTrue(mvd.TryTake(out TestItem? first));
        Assert.IsNotNull(first);
        Assert.AreEqual(11, first!.Value);

        Assert.IsTrue(mvd.TryTake(out TestItem? second));
        Assert.IsNotNull(second);
        Assert.AreEqual(22, second!.Value);

        Assert.IsFalse(mvd.TryTake(out _));
    }

}
