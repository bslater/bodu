// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeDictionaryTests.Add.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class RangeDictionaryTests
{

    // --------------------------------------------------------
    // Add — happy path
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that adding to an empty dictionary inserts the entry as the only entry.
    /// </summary>
    [TestMethod]
    public void Add_WhenDictionaryIsEmpty_ShouldInsertOnly()
    {
        var sut = new RangeDictionary<int, string>();

        sut.Add(0, 10, "tier-A");

        AssertContents(sut, (0, 10, "tier-A"));
    }

    /// <summary>
    /// Verifies that adding with a <see langword="null" /> end is rejected.
    /// </summary>
    [TestMethod]
    public void Add_WhenEndIsNull_ShouldThrowExactly()
    {
        var sut = new RangeDictionary<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            sut.Add("a", null!, 42);
        });
    }

    /// <summary>
    /// Verifies that a failed overlap-rejecting add does not partially mutate the dictionary.
    /// </summary>
    [TestMethod]
    public void Add_WhenOverlapRejected_ShouldNotMutateDictionary()
    {
        RangeDictionary<int, string> sut = CreateDictionary((10, 20, "A"));

        try
        {
            sut.Add(5, 15, "B");
        }
        catch (ArgumentException)
        {
        }

        AssertContents(sut, (10, 20, "A"));
    }

    /// <summary>
    /// Verifies that adding a range that engulfs an existing range is rejected.
    /// </summary>
    [TestMethod]
    public void Add_WhenRangeEngulfsExisting_ShouldThrowExactly()
    {
        RangeDictionary<int, string> sut = CreateDictionary((10, 20, "A"));

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            sut.Add(0, 30, "B");
        });
    }

    /// <summary>
    /// Verifies that adding a range fully contained within an existing range is rejected.
    /// </summary>
    [TestMethod]
    public void Add_WhenRangeIsContainedByExisting_ShouldThrowExactly()
    {
        RangeDictionary<int, string> sut = CreateDictionary((0, 100, "A"));

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            sut.Add(10, 20, "B");
        });
    }

    /// <summary>
    /// Verifies that adding an identical range is rejected.
    /// </summary>
    [TestMethod]
    public void Add_WhenRangeMatchesExisting_ShouldThrowExactly()
    {
        RangeDictionary<int, string> sut = CreateDictionary((0, 10, "A"));

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            sut.Add(0, 10, "B");
        });
    }

    // --------------------------------------------------------
    // Add — overlap rejection
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that adding a range that overlaps the left edge of an existing range is rejected.
    /// </summary>
    [TestMethod]
    public void Add_WhenRangeOverlapsLeftOfExisting_ShouldThrowExactly()
    {
        RangeDictionary<int, string> sut = CreateDictionary((10, 20, "A"));

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            sut.Add(5, 15, "B");
        });
    }

    /// <summary>
    /// Verifies that adding a range that overlaps the right edge of an existing range is rejected.
    /// </summary>
    [TestMethod]
    public void Add_WhenRangeOverlapsRightOfExisting_ShouldThrowExactly()
    {
        RangeDictionary<int, string> sut = CreateDictionary((10, 20, "A"));

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            sut.Add(15, 25, "B");
        });
    }

    /// <summary>
    /// Verifies that adjacent ranges (sharing an endpoint) are allowed and stored as separate entries.
    /// </summary>
    [TestMethod]
    public void Add_WhenRangesAreAdjacent_ShouldKeepAsSeparateEntries()
    {
        var sut = new RangeDictionary<int, string>();

        sut.Add(0, 10, "A");
        sut.Add(10, 20, "B");

        AssertContents(sut, (0, 10, "A"), (10, 20, "B"));
    }

    /// <summary>
    /// Verifies that non-overlapping ranges added out of order produce a sorted result.
    /// </summary>
    [TestMethod]
    public void Add_WhenRangesAreNonOverlappingOutOfOrder_ShouldKeepSortedOrder()
    {
        var sut = new RangeDictionary<int, string>();

        sut.Add(20, 30, "C");
        sut.Add(0, 5, "A");
        sut.Add(10, 15, "B");

        AssertContents(sut, (0, 5, "A"), (10, 15, "B"), (20, 30, "C"));
    }

    /// <summary>
    /// Verifies that adding a degenerate range (<c>start &gt;= end</c>) is rejected.
    /// </summary>
    [TestMethod]
    [DataRow(5, 5)]
    [DataRow(10, 5)]
    public void Add_WhenStartIsNotLessThanEnd_ShouldThrowExactly(int start, int end)
    {
        var sut = new RangeDictionary<int, string>();

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            sut.Add(start, end, "x");
        });
    }
    // --------------------------------------------------------
    // Add(TKey, TKey, TValue) — argument validation
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that adding with a <see langword="null" /> start is rejected.
    /// </summary>
    [TestMethod]
    public void Add_WhenStartIsNull_ShouldThrowExactly()
    {
        var sut = new RangeDictionary<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            sut.Add(null!, "z", 42);
        });
    }

    /// <summary>
    /// Verifies that <see langword="null" /> values are accepted.
    /// </summary>
    [TestMethod]
    public void Add_WhenValueIsNull_ShouldStoreNullValue()
    {
        var sut = new RangeDictionary<int, string?>();

        sut.Add(0, 10, null);

        Assert.AreEqual(1, sut.Count);
        Assert.IsNull(sut[5]);
    }

    // --------------------------------------------------------
    // Add(ValueRange<TKey, TValue>) overload
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that the <see cref="RangeDictionary{TKey, TValue}.Add(ValueRange{TKey, TValue})" /> overload
    /// delegates to the endpoint overload.
    /// </summary>
    [TestMethod]
    public void Add_WhenValueRangeProvided_ShouldDelegateToEndpointOverload()
    {
        var sut = new RangeDictionary<int, string>();

        sut.Add(new ValueRange<int, string>(0, 10, "x"));

        AssertContents(sut, (0, 10, "x"));
    }

}
