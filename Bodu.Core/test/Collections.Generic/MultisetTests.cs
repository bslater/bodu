// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultisetTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Collections.Generic;

/// <summary>
/// Unit tests for <see cref="Multiset{T}"/>.
/// </summary>
[TestClass]
public partial class MultisetTests
{
    // --------------------------------------------------------
    // Constructor — default
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that the default constructor produces an empty multiset with zero count.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Ctor_WhenDefault_ShouldBeEmpty()
    {
        Multiset<string> sut = new Multiset<string>();

        Assert.AreEqual(0, sut.Count);
        Assert.AreEqual(0, sut.DistinctCount);
    }

    /// <summary>
    /// Verifies that the default constructor uses the default equality comparer.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefault_ShouldUseDefaultComparer()
    {
        Multiset<string> sut = new Multiset<string>();

        Assert.AreEqual(EqualityComparer<string>.Default, sut.Comparer);
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
        Multiset<string> sut = new Multiset<string>((IEqualityComparer<string>?)null);

        Assert.AreEqual(EqualityComparer<string>.Default, sut.Comparer);
    }

    /// <summary>
    /// Verifies that a custom comparer is stored and used for element equality.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenComparerIsProvided_ShouldUseSpecifiedComparer()
    {
        Multiset<string> sut = new Multiset<string>(StringComparer.OrdinalIgnoreCase);

        sut.Add("A");
        sut.Add("a");

        Assert.AreEqual(2, sut.Count);
        Assert.AreEqual(1, sut.DistinctCount);
        Assert.AreEqual(2, sut.CountOf("A"));
    }

    // --------------------------------------------------------
    // Constructor — from collection
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that constructing from a null collection throws <see cref="ArgumentNullException"/>.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCollectionIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new Multiset<string>((IEnumerable<string>)null!);
        });
    }

    /// <summary>
    /// Verifies that constructing from a collection correctly tracks element counts.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCollectionHasDuplicates_ShouldTrackMultiplicity()
    {
        Multiset<string> sut = new Multiset<string>(["a", "b", "a", "c", "a"]);

        Assert.AreEqual(5, sut.Count);
        Assert.AreEqual(3, sut.DistinctCount);
        Assert.AreEqual(3, sut.CountOf("a"));
        Assert.AreEqual(1, sut.CountOf("b"));
        Assert.AreEqual(1, sut.CountOf("c"));
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
        Multiset<int> sut = new Multiset<int>();
        sut.Add(1);
        sut.Add(1);
        sut.Add(2);

        Assert.AreEqual(3, sut.Count);
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.DistinctCount"/> reflects only the number of unique elements.
    /// </summary>
    [TestMethod]
    public void DistinctCount_WhenItemsAdded_ShouldReflectUniqueElementCount()
    {
        Multiset<int> sut = new Multiset<int>();
        sut.Add(1);
        sut.Add(1);
        sut.Add(2);

        Assert.AreEqual(2, sut.DistinctCount);
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
        Multiset<string> sut = new Multiset<string>();

        Assert.IsFalse(sut.Contains("x"));
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Contains"/> returns <see langword="true"/> for a present element.
    /// </summary>
    [TestMethod]
    public void Contains_WhenElementPresent_ShouldReturnTrue()
    {
        Multiset<string> sut = new Multiset<string>();
        sut.Add("hello");

        Assert.IsTrue(sut.Contains("hello"));
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Contains"/> returns <see langword="false"/> for a missing element.
    /// </summary>
    [TestMethod]
    public void Contains_WhenElementAbsent_ShouldReturnFalse()
    {
        Multiset<string> sut = new Multiset<string>();
        sut.Add("hello");

        Assert.IsFalse(sut.Contains("world"));
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
        Multiset<int> sut = new Multiset<int>();

        Assert.AreEqual(0, sut.CountOf(42));
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.CountOf"/> returns the correct multiplicity after multiple additions.
    /// </summary>
    [TestMethod]
    public void CountOf_WhenElementAddedMultipleTimes_ShouldReturnMultiplicity()
    {
        Multiset<int> sut = new Multiset<int>();
        sut.Add(5);
        sut.Add(5);
        sut.Add(5);

        Assert.AreEqual(3, sut.CountOf(5));
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
        Multiset<string> sut = new Multiset<string>(["a", "a", "b"]);

        sut.Clear();

        Assert.AreEqual(0, sut.Count);
        Assert.AreEqual(0, sut.DistinctCount);
        Assert.IsFalse(sut.Contains("a"));
    }

    // --------------------------------------------------------
    // CopyTo
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.CopyTo"/> throws <see cref="ArgumentNullException"/> when the array is null.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenArrayIsNull_ShouldThrowArgumentNullException()
    {
        Multiset<int> sut = new Multiset<int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            sut.CopyTo(null!, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.CopyTo"/> throws <see cref="ArgumentOutOfRangeException"/> when the index is negative.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenIndexIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        Multiset<int> sut = new Multiset<int>();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            sut.CopyTo(new int[5], -1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.CopyTo"/> copies all elements with correct multiplicity.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenCalled_ShouldCopyAllElementsWithMultiplicity()
    {
        Multiset<int> sut = new Multiset<int>();
        sut.Add(10, 3);
        sut.Add(20, 2);

        int[] dest = new int[5];
        sut.CopyTo(dest, 0);

        int[] sorted = (int[])dest.Clone();
        System.Array.Sort(sorted);
        CollectionAssert.AreEqual(new[] { 10, 10, 10, 20, 20 }, sorted);
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.CopyTo"/> throws <see cref="ArgumentException"/> when the destination array is too small.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenArrayIsTooSmall_ShouldThrowArgumentException()
    {
        Multiset<int> sut = new Multiset<int>();
        sut.Add(1);
        sut.Add(2);
        sut.Add(3);

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            sut.CopyTo(new int[2], 0);
        });
    }
}
