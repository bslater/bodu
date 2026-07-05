// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BitSetTests.Enumeration.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public partial class BitSetTests
{
    /// <summary>
    /// Verifies that enumeration yields the set-bit indices in ascending order, including bits sharing a word and
    /// bits in distant words.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenBitsSet_ShouldYieldIndicesAscending()
    {
        var bits = CreateWithBits(500, 0, 63, 64, 1);

        CollectionAssert.AreEqual(new[] { 0, 1, 63, 64, 500 }, bits.ToArray());
    }

    /// <summary>
    /// Verifies that enumerating an empty set yields nothing.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenSetIsEmpty_ShouldYieldNothing()
    {
        var bits = new BitSet(256);

        Assert.IsFalse(bits.GetEnumerator().MoveNext());
        Assert.AreEqual(0, bits.ToArray().Length);
    }

    /// <summary>
    /// Verifies that the public <see cref="BitSet.GetEnumerator" /> returns the non-boxing
    /// <see cref="BitSet.Enumerator" /> struct rather than a boxed interface enumerator.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenCalledDirectly_ShouldReturnStructEnumerator()
    {
        var bits = CreateWithBits(2);

        var enumerator = bits.GetEnumerator();

        Assert.IsInstanceOfType<BitSet.Enumerator>(enumerator);
        Assert.IsTrue(typeof(BitSet.Enumerator).IsValueType);
    }

    /// <summary>
    /// Verifies that the explicit <see cref="IEnumerable{T}" /> and <see cref="IEnumerable" /> implementations yield
    /// the same ascending indices as the struct enumerator.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenAccessedThroughInterfaces_ShouldYieldSameIndices()
    {
        var bits = CreateWithBits(1, 70);

        var generic = new List<int>();
        foreach (var index in (IEnumerable<int>)bits)
            generic.Add(index);

        var nonGeneric = new List<object?>();
        foreach (var index in (IEnumerable)bits)
            nonGeneric.Add(index);

        CollectionAssert.AreEqual(new[] { 1, 70 }, generic);
        CollectionAssert.AreEqual(new object[] { 1, 70 }, nonGeneric);
    }

    /// <summary>
    /// Verifies that <see cref="BitSet.Enumerator.Current" /> reports the most recently yielded index.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenMoveNextSucceeds_ShouldExposeCurrentIndex()
    {
        var bits = CreateWithBits(9, 130);
        var enumerator = bits.GetEnumerator();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(9, enumerator.Current);
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(130, enumerator.Current);
        Assert.IsFalse(enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that mutating the set invalidates an active enumerator, causing <c>MoveNext</c> to throw
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenSetMutatedDuringEnumeration_ShouldThrowInvalidOperationException()
    {
        var bits = CreateWithBits(1, 2, 3);
        var enumerator = bits.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());

        bits.Set(10);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = enumerator.MoveNext();
        });
    }

    /// <summary>
    /// Verifies that <see cref="BitSet.Enumerator.Reset" /> restarts the walk from the first set bit, and throws
    /// <see cref="InvalidOperationException" /> after a mutation.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenReset_ShouldRestartFromFirstSetBit()
    {
        var bits = CreateWithBits(4, 90);
        var enumerator = bits.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        Assert.IsTrue(enumerator.MoveNext());

        enumerator.Reset();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(4, enumerator.Current);

        bits.Flip(4);
        var invalidated = enumerator;
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            invalidated.Reset();
        });
    }
}
