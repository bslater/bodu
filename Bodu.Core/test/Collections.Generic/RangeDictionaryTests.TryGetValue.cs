// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeDictionaryTests.TryGetValue.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class RangeDictionaryTests
{

    /// <summary>
    /// Verifies that <see cref="RangeDictionary{TKey, TValue}.TryGetEntry(TKey, out ValueRange{TKey, TValue})" />
    /// on an empty dictionary returns <see langword="false" /> and a default entry.
    /// </summary>
    [TestMethod]
    public void TryGetEntry_WhenDictionaryIsEmpty_ShouldReturnFalseAndDefault()
    {
        var sut = new RangeDictionary<int, string>();

        var found = sut.TryGetEntry(5, out ValueRange<int, string> entry);

        Assert.IsFalse(found);
        Assert.AreEqual(default(ValueRange<int, string>).StartInclusive, entry.StartInclusive);
        Assert.AreEqual(default(ValueRange<int, string>).EndExclusive, entry.EndExclusive);
        Assert.IsNull(entry.Value);
    }

    /// <summary>
    /// Verifies that <see cref="RangeDictionary{TKey, TValue}.TryGetEntry(TKey, out ValueRange{TKey, TValue})" />
    /// returns the full entry when the key is in range.
    /// </summary>
    [TestMethod]
    public void TryGetEntry_WhenKeyIsInRange_ShouldReturnFullEntry()
    {
        RangeDictionary<int, string> sut = CreateDictionary((0, 10, "tier-A"), (20, 30, "tier-B"));

        var found = sut.TryGetEntry(25, out ValueRange<int, string> entry);

        Assert.IsTrue(found);
        Assert.AreEqual(20, entry.StartInclusive);
        Assert.AreEqual(30, entry.EndExclusive);
        Assert.AreEqual("tier-B", entry.Value);
    }

    /// <summary>
    /// Verifies that <see cref="RangeDictionary{TKey, TValue}.TryGetEntry(TKey, out ValueRange{TKey, TValue})" />
    /// returns <see langword="false" /> and a default entry when the key is not in any range.
    /// </summary>
    [TestMethod]
    public void TryGetEntry_WhenKeyIsNotInAnyRange_ShouldReturnFalse()
    {
        RangeDictionary<int, string> sut = CreateDictionary((0, 10, "A"));

        var found = sut.TryGetEntry(99, out ValueRange<int, string> entry);

        Assert.IsFalse(found);
        Assert.IsNull(entry.Value);
    }

    // --------------------------------------------------------
    // TryGetEntry
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="RangeDictionary{TKey, TValue}.TryGetEntry(TKey, out ValueRange{TKey, TValue})" />
    /// rejects a <see langword="null" /> key.
    /// </summary>
    [TestMethod]
    public void TryGetEntry_WhenKeyIsNull_ShouldThrowExactly()
    {
        var sut = new RangeDictionary<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.TryGetEntry(null!, out _);
        });
    }

    /// <summary>
    /// Verifies that <see cref="RangeDictionary{TKey, TValue}.TryGetValue(TKey, out TValue)" /> on an empty
    /// dictionary returns <see langword="false" /> and yields the default value.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenDictionaryIsEmpty_ShouldReturnFalseAndDefaultValue()
    {
        var sut = new RangeDictionary<int, string>();

        var found = sut.TryGetValue(5, out var value);

        Assert.IsFalse(found);
        Assert.IsNull(value);
    }

    /// <summary>
    /// Verifies that <see cref="RangeDictionary{TKey, TValue}.TryGetValue(TKey, out TValue)" /> returns
    /// <see langword="true" /> with the associated value when the key is inside a range.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenKeyIsInRange_ShouldReturnTrueWithValue()
    {
        RangeDictionary<int, string> sut = CreateDictionary((0, 10, "tier-A"), (20, 30, "tier-B"));

        Assert.IsTrue(sut.TryGetValue(5, out var a));
        Assert.AreEqual("tier-A", a);

        Assert.IsTrue(sut.TryGetValue(25, out var b));
        Assert.AreEqual("tier-B", b);
    }

    /// <summary>
    /// Verifies that <see cref="RangeDictionary{TKey, TValue}.TryGetValue(TKey, out TValue)" /> returns
    /// <see langword="false" /> and the default value when no range contains the key.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenKeyIsNotInAnyRange_ShouldReturnFalseAndDefaultValue()
    {
        RangeDictionary<int, string> sut = CreateDictionary((0, 10, "A"));

        var found = sut.TryGetValue(99, out var value);

        Assert.IsFalse(found);
        Assert.IsNull(value);
    }
    // --------------------------------------------------------
    // TryGetValue
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="RangeDictionary{TKey, TValue}.TryGetValue(TKey, out TValue)" /> rejects a
    /// <see langword="null" /> key.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenKeyIsNull_ShouldThrowExactly()
    {
        var sut = new RangeDictionary<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.TryGetValue(null!, out _);
        });
    }

}
