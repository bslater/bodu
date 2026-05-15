// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeSetTests.Ctor.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Collections.Generic;

public partial class RangeSetTests
{

    /// <summary>
    /// Verifies that the collection-and-comparer constructor stores the supplied comparer alongside the source.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCollectionAndComparerProvided_ShouldUseBoth()
    {
        Range<int>[] source = [new Range<int>(0, 5)];

        var sut = new RangeSet<int>(source, Comparer<int>.Default);

        Assert.AreSame(Comparer<int>.Default, sut.Comparer);
        Assert.AreEqual(1, sut.Count);
    }

    /// <summary>
    /// Verifies that the collection constructor adds each source range, merging overlapping ones according to
    /// the same rules as <see cref="RangeSet{T}.Add(T, T)" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCollectionContainsNonOverlappingRanges_ShouldStoreAll()
    {
        Range<int>[] source = [new Range<int>(0, 5), new Range<int>(10, 15), new Range<int>(20, 25)];

        var sut = new RangeSet<int>(source);

        Assert.AreEqual(3, sut.Count);
        AssertContents(sut, (0, 5), (10, 15), (20, 25));
    }

    /// <summary>
    /// Verifies that the collection constructor merges overlapping source ranges into a single combined range.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCollectionContainsOverlappingRanges_ShouldMergeOnInsertion()
    {
        Range<int>[] source = [new Range<int>(0, 5), new Range<int>(3, 10), new Range<int>(8, 12)];

        var sut = new RangeSet<int>(source);

        Assert.AreEqual(1, sut.Count);
        AssertContents(sut, (0, 12));
    }

    /// <summary>
    /// Verifies that the collection constructor with an empty source produces an empty set.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCollectionIsEmpty_ShouldBeEmpty()
    {
        var sut = new RangeSet<int>(Array.Empty<Range<int>>());

        Assert.AreEqual(0, sut.Count);
    }

    // --------------------------------------------------------
    // Collection constructor
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that the collection constructor rejects a <see langword="null" /> source.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCollectionIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new RangeSet<int>((IEnumerable<Range<int>>)null!);
        });
    }

    // --------------------------------------------------------
    // Comparer-only constructor
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that a <see langword="null" /> comparer is replaced with <see cref="Comparer{T}.Default" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenComparerIsNull_ShouldUseDefaultComparer()
    {
        var sut = new RangeSet<int>((IComparer<int>?)null);

        Assert.AreSame(Comparer<int>.Default, sut.Comparer);
    }

    /// <summary>
    /// Verifies that a non-<see langword="null" /> comparer is stored verbatim.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenComparerIsProvided_ShouldUseSpecifiedComparer()
    {
        var comparer = new ReverseIntComparer();

        var sut = new RangeSet<int>(comparer);

        Assert.AreSame(comparer, sut.Comparer);
    }
    // --------------------------------------------------------
    // Default constructor
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that the default constructor produces an empty set using the default comparer.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefault_ShouldBeEmptyWithDefaultComparer()
    {
        var sut = new RangeSet<int>();

        Assert.AreEqual(0, sut.Count);
        Assert.AreEqual(0, sut.Capacity);
        Assert.AreSame(Comparer<int>.Default, sut.Comparer);
    }

}
