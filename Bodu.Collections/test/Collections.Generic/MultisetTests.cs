// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultisetTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

/// <summary>
/// Unit tests for <see cref="Multiset{T}"/>.
/// </summary>
[TestClass]
public partial class MultisetTests
{

    /// <summary>
    /// Verifies that calling <see cref="Multiset{T}.Clear"/> on an already-empty multiset leaves it in a valid empty state.
    /// </summary>
    [TestMethod]
    public void Clear_WhenAlreadyEmpty_ShouldRemainEmpty()
    {
        var mvd = new Multiset<int>();

        mvd.Clear();

        Assert.AreEqual(0, mvd.Count);
        Assert.AreEqual(0, mvd.DistinctCount);
    }

    // --------------------------------------------------------
    // Clear
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Clear"/> resets count and distinct count to zero.
    /// </summary>
    [TestMethod]
    public void Clear_WhenCalled_ShouldRemoveAllElements()
    {
        var mvd = new Multiset<string>(["a", "a", "b"]);

        mvd.Clear();

        Assert.AreEqual(0, mvd.Count);
        Assert.AreEqual(0, mvd.DistinctCount);
        Assert.IsFalse(mvd.Contains("a"));
    }

    /// <summary>
    /// Verifies that items added after <see cref="Multiset{T}.Clear"/> are tracked correctly.
    /// </summary>
    [TestMethod]
    public void Clear_WhenCalledThenItemsReAdded_ShouldTrackNewItems()
    {
        var mvd = new Multiset<string>(["old", "old"]);
        mvd.Clear();
        mvd.Add("new", 3);

        Assert.AreEqual(3, mvd.Count);
        Assert.AreEqual(1, mvd.DistinctCount);
        Assert.AreEqual(3, mvd.CountOf("new"));
        Assert.IsFalse(mvd.Contains("old"));
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Contains"/> returns <see langword="false"/> after all occurrences of an element have been removed.
    /// </summary>
    [TestMethod]
    public void Contains_WhenAllOccurrencesRemoved_ShouldReturnFalse()
    {
        var mvd = new Multiset<string>();
        mvd.Add("x", 3);
        mvd.RemoveAll("x");

        Assert.IsFalse(mvd.Contains("x"));
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Contains"/> returns <see langword="false"/> for a missing element.
    /// </summary>
    [TestMethod]
    public void Contains_WhenElementAbsent_ShouldReturnFalse()
    {
        var mvd = new Multiset<string>();
        mvd.Add("hello");

        Assert.IsFalse(mvd.Contains("world"));
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Contains"/> returns <see langword="true"/> for a present element.
    /// </summary>
    [TestMethod]
    public void Contains_WhenElementPresent_ShouldReturnTrue()
    {
        var mvd = new Multiset<string>();
        mvd.Add("hello");

        Assert.IsTrue(mvd.Contains("hello"));
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Contains"/> returns <see langword="true"/> after an element is re-added following removal.
    /// </summary>
    [TestMethod]
    public void Contains_WhenElementReaddedAfterRemoval_ShouldReturnTrue()
    {
        var mvd = new Multiset<string>();
        mvd.Add("y");
        mvd.Remove("y");
        mvd.Add("y");

        Assert.IsTrue(mvd.Contains("y"));
    }

    // --------------------------------------------------------
    // Contains
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Contains"/> returns <see langword="false"/> for an empty multiset.
    /// </summary>
    [TestMethod]
    public void Contains_WhenEmpty_ShouldReturnFalse()
    {
        var mvd = new Multiset<string>();

        Assert.IsFalse(mvd.Contains("x"));
    }

    // --------------------------------------------------------
    // CopyTo
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.CopyTo"/> throws <see cref="ArgumentNullException"/> when the array is null.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenArrayIsNull_ShouldThrowExactly()
    {
        var mvd = new Multiset<int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            mvd.CopyTo(null!, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.CopyTo"/> throws <see cref="ArgumentException"/> when the destination array is too small.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenArrayIsTooSmall_ShouldThrowExactly()
    {
        var mvd = new Multiset<int>();
        mvd.Add(1);
        mvd.Add(2);
        mvd.Add(3);

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            mvd.CopyTo(new int[2], 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.CopyTo"/> copies all elements with correct multiplicity.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenCalled_ShouldCopyAllElementsWithMultiplicity()
    {
        var mvd = new Multiset<int>();
        mvd.Add(10, 3);
        mvd.Add(20, 2);

        int[] dest = new int[5];
        mvd.CopyTo(dest, 0);

        int[] sorted = (int[])dest.Clone();
        System.Array.Sort(sorted);
        CollectionAssert.AreEqual(new[] { 10, 10, 10, 20, 20 }, sorted);
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.CopyTo"/> throws <see cref="ArgumentOutOfRangeException"/> when the index is negative.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenIndexIsNegative_ShouldThrowExactly()
    {
        var mvd = new Multiset<int>();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            mvd.CopyTo(new int[5], -1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.CopyTo"/> does not modify the destination array when the multiset is empty.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenMultisetIsEmpty_ShouldNotModifyArray()
    {
        var mvd = new Multiset<int>();
        int[] dest = [99, 88];

        mvd.CopyTo(dest, 0);

        Assert.AreEqual(99, dest[0]);
        Assert.AreEqual(88, dest[1]);
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.CopyTo"/> copies elements starting at the specified array index.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenOffsetSpecified_ShouldCopyAtCorrectPosition()
    {
        var mvd = new Multiset<int>();
        mvd.Add(42, 2);
        int[] dest = new int[5];

        mvd.CopyTo(dest, 2);

        Assert.AreEqual(0, dest[0]);
        Assert.AreEqual(0, dest[1]);
        Assert.AreEqual(42, dest[2]);
        Assert.AreEqual(42, dest[3]);
        Assert.AreEqual(0, dest[4]);
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.CopyTo"/> throws <see cref="ArgumentException"/> when the remaining space after the offset is insufficient.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenRemainingSpaceAfterOffsetIsInsufficient_ShouldThrowExactly()
    {
        var mvd = new Multiset<int>();
        mvd.Add(1);
        mvd.Add(2);
        mvd.Add(3);

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            mvd.CopyTo(new int[5], 3);
        });
    }

    // --------------------------------------------------------
    // Count and DistinctCount
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Count"/> reflects the total element count including multiplicity.
    /// </summary>
    [TestMethod]
    public void Count_WhenItemsAdded_ShouldReflectTotalWithMultiplicity()
    {
        var mvd = new Multiset<int>();
        mvd.Add(1);
        mvd.Add(1);
        mvd.Add(2);

        Assert.AreEqual(3, mvd.Count);
    }

    // --------------------------------------------------------
    // CountOf
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.CountOf"/> returns zero for an element not in the multiset.
    /// </summary>
    [TestMethod]
    public void CountOf_WhenElementAbsent_ShouldReturnZero()
    {
        var mvd = new Multiset<int>();

        Assert.AreEqual(0, mvd.CountOf(42));
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.CountOf"/> returns the correct multiplicity after multiple additions.
    /// </summary>
    [TestMethod]
    public void CountOf_WhenElementAddedMultipleTimes_ShouldReturnMultiplicity()
    {
        var mvd = new Multiset<int>();
        mvd.Add(5);
        mvd.Add(5);
        mvd.Add(5);

        Assert.AreEqual(3, mvd.CountOf(5));
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.CountOf"/> returns the decremented multiplicity after one occurrence is removed.
    /// </summary>
    [TestMethod]
    public void CountOf_WhenOneOccurrenceRemovedFromMultiple_ShouldReturnDecremented()
    {
        var mvd = new Multiset<int>();
        mvd.Add(3, 4);
        mvd.Remove(3);

        Assert.AreEqual(3, mvd.CountOf(3));
    }

    /// <summary>
    /// Verifies that constructing from a collection with a custom comparer uses the specified comparer for element equality.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCollectionAndComparerProvided_ShouldUseSpecifiedComparer()
    {
        var mvd = new Multiset<string>(
            ["A", "a", "B"],
            StringComparer.OrdinalIgnoreCase);

        Assert.AreEqual(3, mvd.Count);
        Assert.AreEqual(2, mvd.DistinctCount);
        Assert.AreEqual(2, mvd.CountOf("a"));
        Assert.AreEqual(1, mvd.CountOf("b"));
    }

    /// <summary>
    /// Verifies that constructing from a collection correctly tracks element counts.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCollectionHasDuplicates_ShouldTrackMultiplicity()
    {
        var mvd = new Multiset<string>(["a", "b", "a", "c", "a"]);

        Assert.AreEqual(5, mvd.Count);
        Assert.AreEqual(3, mvd.DistinctCount);
        Assert.AreEqual(3, mvd.CountOf("a"));
        Assert.AreEqual(1, mvd.CountOf("b"));
        Assert.AreEqual(1, mvd.CountOf("c"));
    }

    // --------------------------------------------------------
    // Constructor — from collection
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that constructing from a null collection throws <see cref="ArgumentNullException"/>.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCollectionIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new Multiset<string>((IEnumerable<string>)null!);
        });
    }

    // --------------------------------------------------------
    // Constructor — with comparer
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that passing a null comparer to the comparer constructor defaults to the default equality comparer.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenComparerIsNull_ShouldUseDefaultComparer()
    {
        var mvd = new Multiset<string>((IEqualityComparer<string>?)null);

        Assert.AreEqual(EqualityComparer<string>.Default, mvd.Comparer);
    }

    /// <summary>
    /// Verifies that a custom comparer is stored and used for element equality.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenComparerIsProvided_ShouldUseSpecifiedComparer()
    {
        var mvd = new Multiset<string>(StringComparer.OrdinalIgnoreCase);

        mvd.Add("A");
        mvd.Add("a");

        Assert.AreEqual(2, mvd.Count);
        Assert.AreEqual(1, mvd.DistinctCount);
        Assert.AreEqual(2, mvd.CountOf("A"));
    }
    // --------------------------------------------------------
    // Constructor — default
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that the default constructor produces an empty multiset with zero count.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefault_ShouldBeEmpty()
    {
        var mvd = new Multiset<string>();

        Assert.AreEqual(0, mvd.Count);
        Assert.AreEqual(0, mvd.DistinctCount);
    }

    /// <summary>
    /// Verifies that the default constructor uses the default equality comparer.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefault_ShouldUseDefaultComparer()
    {
        var mvd = new Multiset<string>();

        Assert.AreEqual(EqualityComparer<string>.Default, mvd.Comparer);
    }

    /// <summary>
    /// Verifies that constructing from an empty collection produces an empty multiset.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenEmptyCollectionProvided_ShouldBeEmpty()
    {
        var mvd = new Multiset<string>([]);

        Assert.AreEqual(0, mvd.Count);
        Assert.AreEqual(0, mvd.DistinctCount);
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.DistinctCount"/> reflects only the number of unique elements.
    /// </summary>
    [TestMethod]
    public void DistinctCount_WhenItemsAdded_ShouldReflectUniqueElementCount()
    {
        var mvd = new Multiset<int>();
        mvd.Add(1);
        mvd.Add(1);
        mvd.Add(2);

        Assert.AreEqual(2, mvd.DistinctCount);
    }

}
