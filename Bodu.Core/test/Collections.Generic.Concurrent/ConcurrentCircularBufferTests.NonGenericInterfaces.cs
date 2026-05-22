// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBufferTests.NonGenericInterfaces.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentCircularBufferTests
{

    /// <summary>
    /// Verifies that the non-generic enumerator's <see cref="IEnumerator.Current" /> property returns the same value as the strongly
    /// typed <c>Current</c> after a successful <c>MoveNext</c>, exercising the explicit
    /// <see cref="System.Collections.IEnumerator.Current"/> implementation on
    /// <see cref="ConcurrentCircularBuffer{T}.Enumerator"/>.
    /// </summary>
    [TestMethod]
    public void Enumerator_NonGenericCurrent_AfterMoveNext_ShouldReturnLatestElement()
    {
        var buffer = new ConcurrentCircularBuffer<string>(2);
        buffer.Enqueue("seed");

        using ConcurrentCircularBuffer<string>.Enumerator typed = buffer.GetEnumerator();

        Assert.IsTrue(typed.MoveNext());

        IEnumerator legacy = typed;

        Assert.AreEqual(typed.Current, legacy.Current);
        Assert.AreEqual("seed", legacy.Current);
    }

    /// <summary>
    /// Verifies that the non-generic enumerator's <see cref="IEnumerator.Current" /> property returns the current element after a
    /// successful <see cref="IEnumerator.MoveNext" /> call.
    /// </summary>
    [TestMethod]
    public void Enumerator_NonGenericCurrent_AfterMoveNextThroughInterface_ShouldReturnLatestElement()
    {
        var buffer = new ConcurrentCircularBuffer<string>(2);
        buffer.Enqueue("seed");

        IEnumerator legacy = buffer.GetEnumerator();

        Assert.IsTrue(legacy.MoveNext());
        Assert.AreEqual("seed", legacy.Current);
    }

    /// <summary>
    /// Verifies that the non-generic enumerator's <see cref="IEnumerator.Current" /> property surfaces each element produced by
    /// successive <see cref="IEnumerator.MoveNext" /> calls, walking the entire snapshot through the explicit interface.
    /// </summary>
    [TestMethod]
    public void Enumerator_NonGenericCurrent_WhenWalkedToEnd_ShouldReturnEveryElementInOrder()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Enqueue(new TestItem(3));

        ConcurrentCircularBuffer<TestItem>.Enumerator typed = buffer.GetEnumerator();
        IEnumerator legacy = typed;

        var observed = new List<int>();
        while (legacy.MoveNext())
            observed.Add(((TestItem)legacy.Current!).Value);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, observed);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.Enumerator.Reset" /> rewinds the enumerator so a fresh <c>MoveNext</c> walk
    /// re-yields the snapshot from the beginning.
    /// </summary>
    [TestMethod]
    public void Enumerator_Reset_ShouldRewindToBeginning()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        buffer.Enqueue(new TestItem(10));
        buffer.Enqueue(new TestItem(20));

        ConcurrentCircularBuffer<TestItem>.Enumerator enumerator = buffer.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(10, enumerator.Current.Value);

        enumerator.Reset();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(10, enumerator.Current.Value);
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(20, enumerator.Current.Value);
        Assert.IsFalse(enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that the generic <see cref="IEnumerable{T}.GetEnumerator" /> explicit-interface implementation returns an enumerator
    /// that walks every element in FIFO order, exercising the explicit interface path independently of the public
    /// <c>GetEnumerator</c> overload that returns the value-type <c>Enumerator</c> struct.
    /// </summary>
    [TestMethod]
    public void GenericEnumerableGetEnumerator_WhenAccessedThroughInterface_ShouldYieldAllElementsInOrder()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Enqueue(new TestItem(3));

        IEnumerable<TestItem> generic = buffer;
        var observed = new List<int>();
        using (IEnumerator<TestItem> enumerator = generic.GetEnumerator())
        {
            while (enumerator.MoveNext())
                observed.Add(enumerator.Current.Value);
        }

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, observed);
    }
    /// <summary>
    /// Verifies that the explicit <see cref="ICollection.IsSynchronized" /> property on a
    /// <see cref="ConcurrentCircularBuffer{T}" /> reports <see langword="false" /> — the type manages its own synchronisation.
    /// </summary>
    [TestMethod]
    public void IsSynchronized_WhenAccessed_ShouldReturnFalse()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(2);
        ICollection collection = buffer;

        Assert.IsFalse(collection.IsSynchronized);
    }

    /// <summary>
    /// Verifies that <see cref="ICollection.IsSynchronized" /> remains <see langword="false" /> regardless of whether the buffer is
    /// empty, partially populated, or full.
    /// </summary>
    [TestMethod]
    public void IsSynchronized_WhenBufferStateVaries_ShouldRemainFalse()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        ICollection collection = buffer;

        Assert.IsFalse(collection.IsSynchronized);

        buffer.Enqueue(new TestItem(1));
        Assert.IsFalse(collection.IsSynchronized);

        buffer.Enqueue(new TestItem(2));
        buffer.Enqueue(new TestItem(3));
        Assert.IsFalse(collection.IsSynchronized);

        buffer.Clear();
        Assert.IsFalse(collection.IsSynchronized);
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="IEnumerable.GetEnumerator" /> on a
    /// <see cref="ConcurrentCircularBuffer{T}" /> yields the same elements as the strongly typed enumerator.
    /// </summary>
    [TestMethod]
    public void NonGenericGetEnumerator_WhenBufferHasElements_ShouldYieldAllElementsInOrder()
    {
        var buffer = new ConcurrentCircularBuffer<string>(3);
        buffer.Enqueue("a");
        buffer.Enqueue("b");
        buffer.Enqueue("c");

        IEnumerable nonGeneric = buffer;
        var observed = new List<string>();
        foreach (var item in nonGeneric)
            observed.Add((string)item);

        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, observed);
    }

    /// <summary>
    /// Verifies that <see cref="IEnumerable.GetEnumerator" /> on an empty buffer returns an enumerator that immediately reports
    /// end-of-sequence on the first <see cref="IEnumerator.MoveNext" /> call.
    /// </summary>
    [TestMethod]
    public void NonGenericGetEnumerator_WhenBufferIsEmpty_ShouldReturnEnumeratorThatIsImmediatelyExhausted()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        IEnumerable nonGeneric = buffer;

        IEnumerator enumerator = nonGeneric.GetEnumerator();

        Assert.IsFalse(enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that two enumerators obtained from the non-generic <see cref="IEnumerable.GetEnumerator" /> are independent — exhausting
    /// one does not affect the other.
    /// </summary>
    [TestMethod]
    public void NonGenericGetEnumerator_WhenCalledTwice_ShouldReturnIndependentSnapshots()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        buffer.Enqueue(new TestItem(10));
        buffer.Enqueue(new TestItem(20));

        IEnumerable nonGeneric = buffer;

        IEnumerator first = nonGeneric.GetEnumerator();
        IEnumerator second = nonGeneric.GetEnumerator();

        Assert.IsTrue(first.MoveNext());
        Assert.IsTrue(first.MoveNext());
        Assert.IsFalse(first.MoveNext());

        // The second enumerator is unaffected by the first being exhausted.
        Assert.IsTrue(second.MoveNext());
        Assert.AreEqual(10, ((TestItem)second.Current).Value);
    }

    /// <summary>
    /// Verifies that the explicit <see cref="ICollection.SyncRoot" /> property on a
    /// <see cref="ConcurrentCircularBuffer{T}" /> throws <see cref="NotSupportedException" />, matching the behaviour of the BCL
    /// concurrent collections.
    /// </summary>
    [TestMethod]
    public void SyncRoot_WhenAccessed_ShouldThrowExactly()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(2);
        ICollection collection = buffer;

        Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = collection.SyncRoot;
        });
    }

    /// <summary>
    /// Verifies that <see cref="ICollection.SyncRoot" /> throws <see cref="NotSupportedException" /> on every access — accessing the
    /// property must never lazily create a lock object that a caller could subsequently lock on.
    /// </summary>
    [TestMethod]
    public void SyncRoot_WhenAccessedRepeatedly_ShouldThrowExactly()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(2);
        ICollection collection = buffer;

        for (var i = 0; i < 3; i++)
        {
            Assert.ThrowsExactly<NotSupportedException>(() =>
            {
                _ = collection.SyncRoot;
            });
        }
    }

}
