// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeDictionaryTests.Remove.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class RangeDictionaryTests
{

    /// <summary>
    /// Verifies that <see cref="RangeDictionary{TKey, TValue}.Clear" /> on an empty dictionary remains empty.
    /// </summary>
    [TestMethod]
    public void Clear_WhenAlreadyEmpty_ShouldRemainEmpty()
    {
        var sut = new RangeDictionary<int, string>();

        sut.Clear();

        Assert.AreEqual(0, sut.Count);
    }

    // --------------------------------------------------------
    // Clear
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="RangeDictionary{TKey, TValue}.Clear" /> empties the dictionary.
    /// </summary>
    [TestMethod]
    public void Clear_WhenDictionaryHasEntries_ShouldRemoveAllEntries()
    {
        RangeDictionary<int, string> sut = CreateDictionary((0, 10, "A"), (20, 30, "B"));

        sut.Clear();

        Assert.AreEqual(0, sut.Count);
        Assert.IsFalse(sut.ContainsKey(5));
    }

    /// <summary>
    /// Verifies that entries added after <see cref="RangeDictionary{TKey, TValue}.Clear" /> are tracked
    /// normally and do not conflict with previously stored ranges.
    /// </summary>
    [TestMethod]
    public void Clear_WhenEntriesReAddedAfterClear_ShouldTrackNewEntries()
    {
        RangeDictionary<int, string> sut = CreateDictionary((0, 10, "A"), (20, 30, "B"));

        sut.Clear();
        sut.Add(0, 100, "new");

        AssertContents(sut, (0, 100, "new"));
    }

    // --------------------------------------------------------
    // Remove — behaviour
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that removing from an empty dictionary returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void Remove_WhenDictionaryIsEmpty_ShouldReturnFalse()
    {
        var sut = new RangeDictionary<int, string>();

        var removed = sut.Remove(0, 10);

        Assert.IsFalse(removed);
    }

    /// <summary>
    /// Verifies that removing with a <see langword="null" /> end is rejected.
    /// </summary>
    [TestMethod]
    public void Remove_WhenEndIsNull_ShouldThrowArgumentNullException()
    {
        var sut = new RangeDictionary<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.Remove("a", null!);
        });
    }

    /// <summary>
    /// Verifies that a removed entry is no longer reachable through lookups.
    /// </summary>
    [TestMethod]
    public void Remove_WhenEntryRemoved_ShouldNotBeReachable()
    {
        RangeDictionary<int, string> sut = CreateDictionary((0, 10, "A"), (20, 30, "B"));

        sut.Remove(0, 10);

        Assert.IsFalse(sut.ContainsKey(5));
        Assert.IsFalse(sut.TryGetValue(5, out _));
    }

    /// <summary>
    /// Verifies that removing a middle entry preserves the order of the remaining entries.
    /// </summary>
    [TestMethod]
    public void Remove_WhenMiddleEntryRemoved_ShouldPreserveOrder()
    {
        RangeDictionary<int, string> sut = CreateDictionary((0, 5, "A"), (10, 15, "B"), (20, 25, "C"));

        Assert.IsTrue(sut.Remove(10, 15));

        AssertContents(sut, (0, 5, "A"), (20, 25, "C"));
    }

    /// <summary>
    /// Verifies that removing a range that does not exactly match any entry returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void Remove_WhenRangeDoesNotMatchExactly_ShouldReturnFalse()
    {
        RangeDictionary<int, string> sut = CreateDictionary((0, 10, "A"), (20, 30, "B"));

        Assert.IsFalse(sut.Remove(0, 5));
        Assert.IsFalse(sut.Remove(5, 10));
        Assert.IsFalse(sut.Remove(20, 25));
        Assert.IsFalse(sut.Remove(15, 25));

        AssertContents(sut, (0, 10, "A"), (20, 30, "B"));
    }

    /// <summary>
    /// Verifies that removing a range with exactly matching endpoints removes the corresponding entry.
    /// </summary>
    [TestMethod]
    public void Remove_WhenRangeMatchesExactly_ShouldRemoveEntry()
    {
        RangeDictionary<int, string> sut = CreateDictionary((0, 10, "A"), (20, 30, "B"));

        var removed = sut.Remove(0, 10);

        Assert.IsTrue(removed);
        AssertContents(sut, (20, 30, "B"));
    }

    /// <summary>
    /// Verifies that removing a degenerate range is rejected.
    /// </summary>
    [TestMethod]
    [DataRow(5, 5)]
    [DataRow(10, 5)]
    public void Remove_WhenStartIsNotLessThanEnd_ShouldThrowArgumentException(int start, int end)
    {
        var sut = new RangeDictionary<int, string>();

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = sut.Remove(start, end);
        });
    }
    // --------------------------------------------------------
    // Remove — argument validation
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that removing with a <see langword="null" /> start is rejected.
    /// </summary>
    [TestMethod]
    public void Remove_WhenStartIsNull_ShouldThrowArgumentNullException()
    {
        var sut = new RangeDictionary<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.Remove(null!, "z");
        });
    }

}
