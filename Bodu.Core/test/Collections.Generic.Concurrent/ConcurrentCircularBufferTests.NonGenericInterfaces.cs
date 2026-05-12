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
    /// Verifies that the explicit <see cref="ICollection.IsSynchronized" /> property on a
    /// <see cref="ConcurrentCircularBuffer{T}" /> reports <see langword="false" /> — the type manages its own synchronisation.
    /// </summary>
    [TestMethod]
    public void IsSynchronized_WhenAccessed_ShouldReturnFalse()
    {
        var buffer = new ConcurrentCircularBuffer<int>(2);
        ICollection collection = buffer;

        Assert.IsFalse(collection.IsSynchronized);
    }

    /// <summary>
    /// Verifies that the explicit <see cref="ICollection.SyncRoot" /> property on a
    /// <see cref="ConcurrentCircularBuffer{T}" /> throws <see cref="NotSupportedException" />, matching the behaviour of the BCL
    /// concurrent collections.
    /// </summary>
    [TestMethod]
    public void SyncRoot_WhenAccessed_ShouldThrowExactly()
    {
        var buffer = new ConcurrentCircularBuffer<int>(2);
        ICollection collection = buffer;

        Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = collection.SyncRoot;
        });
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="IEnumerable.GetEnumerator" /> on a
    /// <see cref="ConcurrentCircularBuffer{T}" /> yields the same elements as the strongly typed enumerator.
    /// </summary>
    [TestMethod]
    public void NonGenericGetEnumerator_WhenBufferHasElements_ShouldYieldAllElementsInOrder()
    {
        var buffer = new ConcurrentCircularBuffer<int>(3);
        buffer.Enqueue(1);
        buffer.Enqueue(2);
        buffer.Enqueue(3);

        IEnumerable nonGeneric = buffer;
        var observed = new List<int>();
        foreach (object item in nonGeneric)
            observed.Add((int)item);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, observed);
    }

    /// <summary>
    /// Verifies that the non-generic enumerator's <see cref="IEnumerator.Current" /> returns the same value as the strongly typed
    /// <c>Current</c> after a successful <c>MoveNext</c>.
    /// </summary>
    [TestMethod]
    public void Enumerator_NonGenericCurrent_AfterMoveNext_ShouldReturnLatestElement()
    {
        var buffer = new ConcurrentCircularBuffer<int>(2);
        buffer.Enqueue(99);

        using ConcurrentCircularBuffer<int>.Enumerator typed = buffer.GetEnumerator();
        IEnumerator legacy = typed;

        Assert.IsTrue(typed.MoveNext());
        Assert.AreEqual(99, legacy.Current);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.Enumerator.Reset" /> rewinds the enumerator so a fresh <c>MoveNext</c> walk
    /// re-yields the snapshot from the beginning.
    /// </summary>
    [TestMethod]
    public void Enumerator_Reset_ShouldRewindToBeginning()
    {
        var buffer = new ConcurrentCircularBuffer<int>(3);
        buffer.Enqueue(10);
        buffer.Enqueue(20);

        var enumerator = buffer.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(10, enumerator.Current);

        enumerator.Reset();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(10, enumerator.Current);
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(20, enumerator.Current);
        Assert.IsFalse(enumerator.MoveNext());
    }
}
