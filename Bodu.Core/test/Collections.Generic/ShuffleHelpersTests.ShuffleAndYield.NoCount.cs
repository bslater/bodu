// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ShuffleHelpersTests.ShuffleAndYield.NoCount.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class ShuffleHelpersTests
{

    /// <summary>
    /// Verifies that the count-less <see cref="ShuffleHelpers.ShuffleAndYield{T}(T[], IRandomGenerator)" /> overload returns every
    /// element of the source array in a shuffled order.
    /// </summary>
    [TestMethod]
    public void ShuffleAndYield_Array_NoCount_ShouldReturnAllElements()
    {
        int[] source = Enumerable.Range(1, 8).ToArray();
        int[] actual = ShuffleHelpers.ShuffleAndYield(source, new XorShiftRandom(7)).ToArray();

        Assert.HasCount(source.Length, actual);
        CollectionAssert.AreEquivalent(source, actual);
        CollectionAssert.AllItemsAreUnique(actual);
    }

    /// <summary>
    /// Verifies that the count-less <see cref="ShuffleHelpers.ShuffleAndYield{T}(T[], IRandomGenerator)" /> overload throws
    /// <see cref="ArgumentNullException" /> when the array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ShuffleAndYield_Array_NoCount_WhenArrayIsNull_ShouldThrowExactly()
    {
        int[] array = null!;
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = ShuffleHelpers.ShuffleAndYield(array, new XorShiftRandom()).ToArray();
        });
    }
    /// <summary>
    /// Verifies that the count-less <see cref="ShuffleHelpers.ShuffleAndYield{T}(IEnumerable{T}, IRandomGenerator)" /> overload returns
    /// every element of the source sequence in a shuffled order.
    /// </summary>
    [TestMethod]
    public void ShuffleAndYield_IEnumerable_NoCount_ShouldReturnAllElements()
    {
        var source = Enumerable.Range(1, 10).ToList();
        int[] actual = ShuffleHelpers.ShuffleAndYield(source.AsEnumerable(), new XorShiftRandom(7)).ToArray();

        Assert.HasCount(source.Count, actual);
        CollectionAssert.AreEquivalent(source, actual);
        CollectionAssert.AllItemsAreUnique(actual);
    }

    /// <summary>
    /// Verifies that the count-less <see cref="ShuffleHelpers.ShuffleAndYield{T}(IEnumerable{T}, IRandomGenerator)" /> overload throws
    /// <see cref="ArgumentNullException" /> when the source is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ShuffleAndYield_IEnumerable_NoCount_WhenSourceIsNull_ShouldThrowExactly()
    {
        IEnumerable<int> source = null!;
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = ShuffleHelpers.ShuffleAndYield(source, new XorShiftRandom()).ToArray();
        });
    }

#if !NETSTANDARD2_0

    /// <summary>
    /// Verifies that the count-less <see cref="ShuffleHelpers.ShuffleAndYield{T}(ReadOnlySpan{T}, IRandomGenerator)" /> overload returns
    /// every element from the span.
    /// </summary>
    [TestMethod]
    public void ShuffleAndYield_ReadOnlySpan_NoCount_ShouldReturnAllElements()
    {
        int[] underlying = Enumerable.Range(1, 6).ToArray();
        ReadOnlySpan<int> span = underlying;
        int[] actual = ShuffleHelpers.ShuffleAndYield(span, new XorShiftRandom(7)).ToArray();

        Assert.HasCount(underlying.Length, actual);
        CollectionAssert.AreEquivalent(underlying, actual);
        CollectionAssert.AllItemsAreUnique(actual);
    }

    /// <summary>
    /// Verifies that the count-less <see cref="ShuffleHelpers.ShuffleAndYield{T}(Memory{T}, IRandomGenerator)" /> overload returns every
    /// element from the memory block.
    /// </summary>
    [TestMethod]
    public void ShuffleAndYield_Memory_NoCount_ShouldReturnAllElements()
    {
        int[] underlying = Enumerable.Range(1, 6).ToArray();
        Memory<int> memory = underlying;
        int[] actual = ShuffleHelpers.ShuffleAndYield(memory, new XorShiftRandom(7)).ToArray();

        Assert.HasCount(underlying.Length, actual);
        CollectionAssert.AreEquivalent(underlying, actual);
        CollectionAssert.AllItemsAreUnique(actual);
    }

#endif

}
