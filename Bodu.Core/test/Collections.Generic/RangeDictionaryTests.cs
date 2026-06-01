// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeDictionaryTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

/// <summary>
/// Unit tests for <see cref="RangeDictionary{TKey, TValue}" />.
/// </summary>
[TestClass]
public partial class RangeDictionaryTests
{

    /// <summary>
    /// Verifies the happy-path smoke check: adding a range and querying it through the indexer returns the
    /// associated value.
    /// </summary>
    [TestMethod]
    public void Add_WhenSingleRangeAdded_ShouldReturnValueViaIndexer()
    {
        var sut = new RangeDictionary<int, string>();

        sut.Add(0, 10, "tier-A");

        Assert.AreEqual(1, sut.Count);
        Assert.AreEqual("tier-A", sut[5]);
    }

    /// <summary>
    /// Asserts that the contents of <paramref name="dict" /> match the specified expected entries in order.
    /// </summary>
    /// <param name="dict">The dictionary under test.</param>
    /// <param name="expected">The expected ordered entries.</param>
    private static void AssertContents(
        RangeDictionary<int, string> dict,
        params (int Start, int End, string Value)[] expected)
    {
        Assert.AreEqual(expected.Length, dict.Count);
        for (var i = 0; i < expected.Length; i++)
        {
            ValueRange<int, string> actual = dict.GetEntryAt(i);
            Assert.AreEqual(expected[i].Start, actual.StartInclusive);
            Assert.AreEqual(expected[i].End, actual.EndExclusive);
            Assert.AreEqual(expected[i].Value, actual.Value);
        }
    }

    /// <summary>
    /// Creates a <see cref="RangeDictionary{TKey, TValue}" /> seeded with the specified
    /// <c>(start, end, value)</c> tuples.
    /// </summary>
    /// <param name="entries">The entries to seed in order.</param>
    /// <returns>The populated dictionary.</returns>
    private static RangeDictionary<int, string> CreateDictionary(params (int Start, int End, string Value)[] entries)
    {
        var dict = new RangeDictionary<int, string>();
        foreach ((int Start, int End, string Value) entry in entries)
            dict.Add(entry.Start, entry.End, entry.Value);

        return dict;
    }

}
